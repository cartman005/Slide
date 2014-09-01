using Kozlowski.Slide.Background;
using Kozlowski.Slide.Common;
using Kozlowski.Slide.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Kozlowski.Slide
{
    /// <summary>
    /// The main and only page of Slide. Contains a FlipView which displays the selected images.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private List<StorageFile> fileList;
        private DispatcherTimer timer;
        private Settings settings;
        private bool isPaused;       

        /// <summary>
        /// The collection of images to be displayed for the slideshow.
        /// </summary>
        public ObservableCollection<ListItem> Items { get; set; }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and process lifetime management.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += NavigationHelper_LoadState;
            this.navigationHelper.SaveState += NavigationHelper_SaveState;

            settings = Settings.Instance;
            settings.PropertyChanged += Settings_Changed;

            //Items = new ObservableCollection<ListItem>();
            //FlipView.ItemsSource = Items;

            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            isPaused = true;

            fileList = new List<StorageFile>();
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also provided when recreating
        /// a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">
        /// Event data that provides both the navigation parameter passed to <see cref="Frame.Navigate(Type, Object)"/>
        /// when this page was initially requested and a dictionary of state preserved by this page during an earlier session.
        /// The state will be null the first time a page is visited.
        /// </param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Debug.WriteLine("Loading state");

            // Set up timer first in case it gets started
            timer.Interval = TimeSpan.FromSeconds(settings.Interval);
            Debug.WriteLine("Timer set to " + settings.Interval);

            // TODO Load Items from saved state.

            // Try to load previous Index and Items collection
            int initialIndex = -1;
            if (e.PageState != null && e.PageState.ContainsKey("SelectedIndex"))
            {
                object storedIndex = null;
                if (e.PageState.TryGetValue("SelectedIndex", out storedIndex))
                {
                    try
                    {
                        StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(Constants.CollectionFileName);
                        if (file != null)
                        {
                            using (IInputStream inStream = await file.OpenSequentialReadAsync())
                            {
                                DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<ListItem>));
                                var data = (ObservableCollection<ListItem>)serializer.ReadObject(inStream.AsStreamForRead());
                                Items = data;

                                Debug.WriteLine("Found serialized Items collection");
                            }

                            Debug.WriteLine("Set index to " + (int)storedIndex);
                            initialIndex = (int)storedIndex;
                        }
                    }
                    catch (FileNotFoundException ex)
                    {
                        Debug.WriteLine("File not found {0}", ex.Source);
                    }
                }
            }
           
            if (Items == null)
                Items = new ObservableCollection<ListItem>();

            FlipView.ItemsSource = Items;
            FlipView.SelectedIndex = initialIndex;
                        
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
            isPaused = false; // Should be set before calling LoadMoreFiles
            await LoadMoreFiles(Constants.ImagesToLoad);
            UpdateName((ListItem)FlipView.SelectedItem);
            FlipView.SelectionChanged += FlipView_SelectionChanged;

            // Register background task
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                RegisterTimerTask((uint)Constants.BackgroundTaskInterval);
                RegisterUserTask();
            }

            // Set the input focus to ensure that keyboard events are raised
            //this.Loaded += delegate { this.Focus(FocusState.Programmatic); };
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            // Create first set of tile updates
            if (!settings.InitialUpdatesMade)
            {
                Debug.WriteLine("Generate initial updates");
                await TileMaker.CreateTiles(settings.Interval, fileList);
                settings.InitialUpdatesMade = true;
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the page is discarded
        /// from the navigation cache. Values must conform to the serialization.
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with serializable state.
        /// </param>
        private async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            Debug.WriteLine("Saving state");

            // TODO Serialize and save Items.

            // Save the maxIndex variable
            e.PageState.Add("SelectedIndex", FlipView.SelectedIndex);

            // Save Items collection
            MemoryStream sessionData = new MemoryStream();
            DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<ListItem>));
            serializer.WriteObject(sessionData, Items);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.CollectionFileName, CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                sessionData.Seek(0, SeekOrigin.Begin);
                await sessionData.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }

            Debug.WriteLine("Finished saving state");
        }

        /// <summary>
        /// Called whenever the FlipView selection moves forward.
        /// Checks if more files need to be loaded into the collection.
        /// </summary>
        private async void MoveForward()
        {
            Debug.WriteLine("Move forward");
            //if (Items.Count > 0)
            //{
                // Buffer of 3 images due to the asynchronous execution of this method
                if (Items.Count - FlipView.SelectedIndex < 3)
                {
                    await LoadMoreFiles(Constants.ImagesToLoad);
                }
           // }
        }

        /// <summary>
        /// Resets the tick interval of the timer.
        /// </summary>
        private void ResetTimer()
        {
            Debug.WriteLine("Reset Timer");
            if (!isPaused)
            {
                timer.Stop();
                timer.Start();
            }
        }

        /// <summary>
        /// Updates the chosen images to match updated setting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Settings_Changed(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Settings Changed");
            timer.Interval = TimeSpan.FromSeconds(settings.Interval);

            if (!timer.IsEnabled)
                timer.Start();

            // Change collection of image files to use if the changed property affects it
            if (e.PropertyName == "FolderPath" || e.PropertyName == "IncludeSubfolders" || e.PropertyName == "Shuffle")
            {
                // Don't clear out previous images
                //Items.Clear();
                //maxIndex = 0;
                fileList.Clear();
                fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
                await LoadMoreFiles(Constants.ImagesToLoad);
            }

            // Regenerate tile updates
            await TileMaker.CreateTiles(settings.Interval, fileList);
            settings.InitialUpdatesMade = true;
        }

        private async Task LoadMoreFiles(int count)
        {
            Debug.WriteLine("Load more files");
            int index;

            for (; count > 0; count--)
            {
                // Check if more files need to be loaded
                if (fileList.Count <= 0)
                    fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));

                // Set index depending on shuffle setting
                if (settings.Shuffle)
                    index = SingleRandom.Instance.Next(0, fileList.Count);
                else
                {
                    index = 0;
                }

                // Add to Items if enough files were found
                if (fileList.Count >= index + 1)
                {
                    Items.Add(new ListItem { FilePath = fileList[index].Path, Name = fileList[index].DisplayName });
                    fileList.RemoveAt(index);
                    //maxIndex++;
                }
                else
                {
                    Debug.WriteLine("No images found");
                    Pause_Click(null, null);

                    // Create the message dialog and set its content
                    var messageDialog = new MessageDialog("No image files were found in the source folder.");

                    // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                    messageDialog.Commands.Add(new UICommand(
                        "Try again",
                        new UICommandInvokedHandler(this.CommandInvokedHandler)));
                    messageDialog.Commands.Add(new UICommand(
                        "Close",
                        new UICommandInvokedHandler(this.CommandInvokedHandler)));

                    // Set the command that will be invoked by default
                    messageDialog.DefaultCommandIndex = 0;

                    // Set the command to be invoked when escape is pressed
                    messageDialog.CancelCommandIndex = 1;

                    // Show the message dialog
                    await messageDialog.ShowAsync();

                    return;
                }
            }

            // Ensure collection does not become too large for memory
            if (Items.Count > Constants.MaxCount)
            {
                // If the collection reaches the maximum, remove three times the load amount
                for (int currentCount = Items.Count; currentCount > Constants.MaxCount - (3 * Constants.ImagesToLoad); currentCount--)
                {
                    Items.RemoveAt(0);
                }
            }

            return;
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            switch (command.Label)
            {
                case "Try again":
                    MoveForward();
                    Play_Click(null, null);
                    break;

                case "Close":
                    // Do nothing
                    break;
            }
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Left:
                    ResetTimer();
                    break;
                case VirtualKey.Right:
                    ResetTimer();
                    break;
                case VirtualKey.Space:
                    if (timer.IsEnabled)
                        Pause_Click(null, null);
                    else
                        Play_Click(null, null);
                    break;
            }
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(((ListItem)FlipView.SelectedItem).FilePath);
            LauncherOptions launcherOptions = new LauncherOptions();
            launcherOptions.DisplayApplicationPicker = true;
            await Launcher.LaunchFileAsync(file, launcherOptions);
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Clear images");
          
            FlipView.SelectionChanged -= FlipView_SelectionChanged;
            fileList.Clear();
            Items.Clear();
            await LoadMoreFiles(Constants.ImagesToLoad);
            UpdateName((ListItem)FlipView.SelectedItem);
            FlipView.SelectionChanged += FlipView_SelectionChanged;
            Debug.WriteLine("Selected index is " + FlipView.SelectedIndex);
            ResetTimer();

            // TODO Reset tiles as well
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            isPaused = false;
            if (MainAppBar.IsOpen)
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Visible;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Collapsed;
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            isPaused = true;
            if (MainAppBar.IsOpen)
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Collapsed;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Visible;
            }
        }

        private void FlipView_SelectionChanged(object sender, object e)
        {
            Debug.WriteLine("Selection changed, index " + FlipView.SelectedIndex);
            //Debug.WriteLine("Change to " + Items[FlipView.SelectedIndex]);

            ResetTimer();

            // Check if more files need to be loaded
            MoveForward();

            // Update text with the name of the image
            UpdateName((ListItem)FlipView.SelectedItem);
        }

        private void UpdateName(ListItem item)
        {
            if (item != null)
                FileName.Text = item.Name;
        }

        private async void Timer_Tick(object sender, object e)
        {
            Debug.WriteLine("Timer tick");

            // Check that the next image is available
            // Additional images will be loaded by the FlipView_SelectionChanged event
            if (FlipView.SelectedIndex >= Items.Count)
                await LoadMoreFiles(Constants.ImagesToLoad);
            
            // Advance the flipview selection
            FlipView.SelectedIndex++;
        }

        private void MainAppBar_Opened(object sender, object e)
        {
            timer.Stop();
            if (isPaused)
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Collapsed;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Visible;
            }
            else
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Visible;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Collapsed;
            }
        }

        private void MainAppBar_Closed(object sender, object e)
        {
            if (!isPaused)
                timer.Start();
        }

        /// <summary>
        /// Registers the timer background task that runs at the given frequency.
        /// 15 minutes is the most frequent interval that a background event can be scheduled for.
        /// </summary>
        private void RegisterTimerTask(uint minutes)
        {
            // Check if the task already exists
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == Constants.TimerTaskName)
                    return;
            }

            // Create the task
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = Constants.TimerTaskName;
            builder.TaskEntryPoint = Constants.TaskEntry;
            builder.SetTrigger(new TimeTrigger(minutes, false));
            var registration = builder.Register();
        }

        /// <summary>
        /// Registers the user present background task that runs when the user logs back onto the device.
        /// </summary>
        private void RegisterUserTask()
        {
            // Check if the task already exists
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == Constants.UserTaskName)
                    return;
            }
            
            // Create the task
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = Constants.UserTaskName;
            builder.TaskEntryPoint = Constants.TaskEntry;
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
            var registration = builder.Register();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // FlipView keeps three items in memory at a time, so force the update the size of each so they are resized
            // Other items are not in memory and will be resized when they are loaded
            int startingIndex;

            // Grid_SizeChanged is called on initialization, so make sure that the SelectedIndex is not negative
            if (FlipView.SelectedIndex >= 0)
            {
                if (FlipView.SelectedIndex > 0)
                    startingIndex = FlipView.SelectedIndex - 1;
                else
                    startingIndex = FlipView.SelectedIndex;

                for (int i = 0; i < 3; i++)
                {
                    if (startingIndex + i <= Items.Count)
                    {
                        //Debug.WriteLine("Update the size of " + Items[startingIndex + i].Name);
                        Items[startingIndex + i].FilePath = Items[startingIndex + i].FilePath;
                    }
                }
            }
        }
    }
}
