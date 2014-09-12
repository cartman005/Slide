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
using System.Xml;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Kozlowski.Slide
{
    /// <summary>
    /// The main and only page of Slide. Contains a FlipView which displays the selected images.
    /// This displays a full screen slide show of the selected images.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper _navigationHelper;
        private ObservableDictionary _defaultViewModel = new ObservableDictionary();
        private List<StorageFile> _fileList;
        private DispatcherTimer _timer;
        private bool _isPaused;
        private Storyboard _story;

        /// <summary>
        /// The collection of images to be displayed for the slideshow.
        /// </summary>
        public ObservableCollection<ListItem> Items { get; set; }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this._defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and process lifetime management.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += NavigationHelper_LoadState;
            this._navigationHelper.SaveState += NavigationHelper_SaveState;
            
            GetSettings().PropertyChanged += Settings_Changed;

            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _isPaused = true;

            _fileList = new List<StorageFile>();
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
            _timer.Interval = TimeSpan.FromSeconds(GetSettings().Interval);
            Debug.WriteLine("Timer set to {0}", GetSettings().Interval);

            // Try to load previous Index, Items collection and paused state
            int initialIndex = -1;
            bool wasPaused = false;
            if (e.PageState != null)
            {
                // Items collection
                if (e.PageState.ContainsKey(Constants.SettingsName_SelectedIndex))
                {
                    object storedIndex = null;
                    if (e.PageState.TryGetValue(Constants.SettingsName_SelectedIndex, out storedIndex))
                    {
                        try
                        {
                            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(Constants.CollectionFileName);
                            if (file != null)
                            {
                                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                                {
                                    DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<ListItem>));
                                    var data = (ObservableCollection<ListItem>)serializer.ReadObject(inStream.AsStreamForRead());
                                    Items = data;
                                    Debug.WriteLine("Found serialized Items collection");
                                }

                                Debug.WriteLine("Start at index {0}", (int)storedIndex);
                                initialIndex = (int)storedIndex;
                            }
                        }
                        catch (FileNotFoundException ex)
                        {
                            Debug.WriteLine("File not found: '{0}'", ex);
                        }
                        catch (XmlException ex)
                        {
                            // File not written properly
                            Debug.WriteLine("Unexpected end of file: '{0}'", ex);
                        }
                    }
                }

                // Paused state
                if (e.PageState.ContainsKey(Constants.SettingsName_IsPaused))
                {
                    object storedPaused = null;
                    if (e.PageState.TryGetValue(Constants.SettingsName_IsPaused, out storedPaused))
                        wasPaused = (bool)storedPaused;
                }
            }

            if (Items == null)
                Items = new ObservableCollection<ListItem>();

            FlipView.ItemsSource = Items;
            FlipView.SelectedIndex = initialIndex;

            _fileList.AddRange(await TileMaker.GetImageList(GetSettings().RootFolder, GetSettings().IncludeSubfolders, GetSettings().Shuffle));
            _isPaused = wasPaused; // Should be set before calling LoadMoreFiles
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
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            FlipView.Focus(Windows.UI.Xaml.FocusState.Programmatic);

            // Hide and show the appropriate AppBar buttons
            TogglePlayPauseButton(_isPaused);

            if (GetSettings().Animate)
                Animate(GetSettings().Interval);

            // Create first set of tile updates
            if (!GetSettings().InitialUpdatesMade)
            {
                Debug.WriteLine("Generate initial updates");
                await TileMaker.CreateTileUpdates(GetSettings().TileNumber, true);
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

            // Save the SelectedIndex and IsPaused
            e.PageState.Add(Constants.SettingsName_SelectedIndex, FlipView.SelectedIndex);
            e.PageState.Add(Constants.SettingsName_IsPaused, _isPaused);

            // Save Items collection
            var sessionData = new MemoryStream();
            var serializer = new DataContractSerializer(typeof(ObservableCollection<ListItem>));
            serializer.WriteObject(sessionData, Items);

            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.CollectionFileName, CreationCollisionOption.ReplaceExisting);
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

            // Use a buffer of loaded images due to the asynchronous execution of this method
            if (Items.Count - FlipView.SelectedIndex < Constants.ImageLoadBuffer)
            {
                await LoadMoreFiles(Constants.ImagesToLoad);
            }
        }

        /// <summary>
        /// Resets the tick interval of the timer.
        /// </summary>
        private void ResetTimer()
        {
            Debug.WriteLine("Reset Timer");
            if (!_isPaused)
            {
                _timer.Stop();
                _timer.Start();
            }
        }
              
        private async Task LoadMoreFiles(int count)
        {
            Debug.WriteLine("Load more files");
            int index;

            for (; count > 0; count--)
            {
                // Check if more files need to be loaded
                if (_fileList.Count <= 0)
                    _fileList.AddRange(await TileMaker.GetImageList(GetSettings().RootFolder, GetSettings().IncludeSubfolders, GetSettings().Shuffle));

                // Set index depending on shuffle setting
                if (GetSettings().Shuffle)
                    index = SingleRandom.Instance.Next(0, _fileList.Count);
                else
                {
                    index = 0;
                }

                // Add to Items if enough files were found
                if (_fileList.Count >= index + 1)
                {
                    Items.Add(new ListItem { FilePath = _fileList[index].Path, Name = _fileList[index].DisplayName });
                    _fileList.RemoveAt(index);
                }
                else
                {
                    Debug.WriteLine("No images found");
                    Pause();

                    // Create the message dialog and set its content
                    var messageDialog = new MessageDialog("No image files were found in the source folder.");

                    // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
                    messageDialog.Commands.Add(new UICommand(
                        Constants.TRY_AGAIN,
                        new UICommandInvokedHandler(this.CommandInvokedHandler)));
                    messageDialog.Commands.Add(new UICommand(
                        Constants.CLOSE,
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
            _navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        // This function was adapted from a post by Iris Classon on 06/28/2012 on her blog "In Love With Code" here:
        // http://irisclasson.com/2012/06/28/creating-a-scaletransform-animation-in-c-for-winrt-metro-apps/
        private void Animate(int seconds)
        {
            var container = FlipView.ContainerFromIndex(FlipView.SelectedIndex);
            var children = Children(container);
            var img = children.Find(x => x.Name.Equals("MainImage"));

            if (img == null)
                return;

            img.RenderTransform = new ScaleTransform();
            img.RenderTransformOrigin = RandomPoint();

            _story = new Storyboard();

            var xAnim = new DoubleAnimation();
            var yAnim = new DoubleAnimation();

            xAnim.AutoReverse = false;
            yAnim.AutoReverse = false;

            xAnim.Duration = TimeSpan.FromSeconds(seconds);
            yAnim.Duration = TimeSpan.FromSeconds(seconds);

            xAnim.To = Constants.ScaleDecimal;
            yAnim.To = Constants.ScaleDecimal;

            _story.Children.Add(xAnim);
            _story.Children.Add(yAnim);

            Storyboard.SetTarget(xAnim, img);
            Storyboard.SetTarget(yAnim, img);

            Storyboard.SetTargetProperty(xAnim, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
            Storyboard.SetTargetProperty(yAnim, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

            if (!_isPaused)
                _story.Begin();
        }

        private Point RandomPoint()
        {
            var point = new Point();

            var rand = SingleRandom.Instance;

            int x = rand.Next(0, 3);
            int y = rand.Next(0, 3);

            // Set X
            switch(x)
            {
                case 0:
                    point.X = 0.0;
                    break;
                case 1:
                    point.X = 0.5;
                    break;
                case 2:
                    point.X = 1.0;
                    break;
            }

            // Set Y
            switch (y)
            {
                case 0:
                    point.Y = 0.0;
                    break;
                case 1:
                    point.Y = 0.5;
                    break;
                case 2:
                    point.Y = 1.0;
                    break;
            }

            return point;
        }
               
        /// <summary>
        /// Recursively locates all child Image elements of the given XAML parent object.
        /// This function was adapted from a post by Jerry Nixon on 05/08/2013 on StackOverflow here:
        /// http://stackoverflow.com/questions/16375375/how-do-i-access-a-control-inside-a-xaml-datatemplate
        /// </summary>
        /// <param name="parent">The parent XAML element.</param>
        /// <returns>A list containing the children Image elements of the given parent.</returns>
        private List<Image> Children(DependencyObject parent)
        {
            var list = new List<Image>();

            if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child is Image)
                        list.Add(child as Image);
                    list.AddRange(Children(child));
                }
            }
            return list;
        }

        private void UpdateName(ListItem item)
        {
            if (item != null)
                FileName.Text = item.Name;
        }

        private void Play()
        {
            _timer.Start();
            _isPaused = false;

            if (_story != null)
                _story.Begin();
        }

        private void Pause()
        {
            _timer.Stop();
            _isPaused = true;

            if (_story != null)
                _story.Stop();
        }

        private void TogglePlayPauseButton(bool showPlayButton)
        {
            var toolTip = new ToolTip();

            if (showPlayButton)
            {
                PlayPauseButton.Label = "Play";
                PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
                toolTip.Content = "Resume the slideshow";
            }
            else
            {
                PlayPauseButton.Label = "Pause";
                PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
                toolTip.Content = "Pause the slideshow";
            }

            ToolTipService.SetToolTip(PlayPauseButton, toolTip);

            MainAppBar.UpdateLayout();
        }
   
        #region Event handlers
        /// <summary>
        /// Handles the user response from Message Dialogs.
        /// </summary>
        /// <param name="command">Contains the label of the command that was chosen.</param>
        private void CommandInvokedHandler(IUICommand command)
        {
            switch (command.Label)
            {
                case Constants.TRY_AGAIN:
                    // Try loading next image again
                    MoveForward();
                    Play();
                    break;

                case Constants.CLOSE:
                    // Do nothing
                    break;
            }
        }

        /// <summary>
        /// Handles keyboard input that is not already built into the FlipView.
        /// Resets the timer when the left and right arrow keys are used. (The FlipView already responds to these by moving to the next/previous item.)
        /// Plays/pauses the timer when the space bar is pressed.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="args">Contains the key that was pressed.</param>
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
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
                    if (_timer.IsEnabled)
                        Pause();
                    else
                        Play();
                    break;
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
                Play();
            else
                Pause();

            TogglePlayPauseButton(_isPaused);
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Clear images");

            FlipView.SelectionChanged -= FlipView_SelectionChanged;
            _fileList.Clear();
            Items.Clear();
            await LoadMoreFiles(Constants.ImagesToLoad);
            UpdateName((ListItem)FlipView.SelectedItem);
            FlipView.SelectionChanged += FlipView_SelectionChanged;
            Debug.WriteLine("Selected index is {0}", FlipView.SelectedIndex);
            ResetTimer();

            // Regenerate tile updates
            await TileMaker.CreateTileUpdates(GetSettings().TileNumber, true);
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var file = await StorageFile.GetFileFromPathAsync(((ListItem)FlipView.SelectedItem).FilePath);
            var launcherOptions = new LauncherOptions();
            launcherOptions.DisplayApplicationPicker = true;
            await Launcher.LaunchFileAsync(file, launcherOptions);
        }

        /// <summary>
        /// Updates the set of chosen images for the full screen slide show to match updated settings.
        /// This handler is not responsible for updating the images used for the tiles.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="e">Contains details about the property that was changed.</param>
        private async void Settings_Changed(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Settings Changed");

            // Interval
            if (e.PropertyName == Constants.SettingsName_Interval)
                _timer.Interval = TimeSpan.FromSeconds(GetSettings().Interval);

            // Change storyboard if animation or interval settings change
            if (e.PropertyName == Constants.SettingsName_Animate || e.PropertyName == Constants.SettingsName_Interval)
            {
                if (GetSettings().Animate)
                    Animate(GetSettings().Interval);
            }

            // Change collection of image files to use if the changed property affects it
            if (e.PropertyName == Constants.SettingsName_ImagesLocation || e.PropertyName == Constants.SettingsName_Subfolders || e.PropertyName == Constants.SettingsName_Shuffle)
            {
                // Clear images following the current index, leave images that were already passed
                FlipView.SelectionChanged -= FlipView_SelectionChanged;
                for (int i = FlipView.SelectedIndex + 1; i < Items.Count; )
                {
                    Items.RemoveAt(i);
                }

                // Clear file list, as files are missing
                _fileList.Clear();
                _fileList.AddRange(await TileMaker.GetImageList(GetSettings().RootFolder, GetSettings().IncludeSubfolders, GetSettings().Shuffle));
                await LoadMoreFiles(Constants.ImagesToLoad);
                FlipView.SelectionChanged += FlipView_SelectionChanged;
            }

            // Start the timer if not paused
            if (!_isPaused && !_timer.IsEnabled)
                _timer.Start();
        }

        /// <summary>
        /// Handles the selection of the FlipView being changed.
        /// This event is triggered on startup and when the FlipView is moved forward or backward by mouse, touch, keyboard and timer events.
        /// Resets the animation, timer and loads more files if necessary.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="e">Unused parameter.</param>
        private void FlipView_SelectionChanged(object sender, object e)
        {
            Debug.WriteLine("Selection changed, index {0}", FlipView.SelectedIndex);

            if (FlipView.SelectedItem == null)
                return;

            // Reset the animation
            if (_story != null)
                _story.Stop();

            if (GetSettings().Animate)
                Animate(GetSettings().Interval);

            // Reset the timer to its starting interval
            ResetTimer();

            // Check if more files need to be loaded
            MoveForward();

            // Update text with the name of the image
            UpdateName((ListItem)FlipView.SelectedItem);
        }

        /// <summary>
        /// Handles timer tick events by moving to the next image and loading more images when necessary.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="e">Unused parameter.</param>
        private async void Timer_Tick(object sender, object e)
        {
            Debug.WriteLine("Timer tick");

            // Check that the next image is available
            // Additional images will be loaded by the FlipView_SelectionChanged event, so this should not be necessary
            if (FlipView.SelectedIndex >= Items.Count)
                await LoadMoreFiles(Constants.ImagesToLoad);

            // Advance the flipview selection
            FlipView.SelectedIndex++;
        }

        /// <summary>
        /// Updates the size of the Image being displayed when the size of the Grid changes due to the app being snapped.
        /// </summary>
        /// <param name="sender">Usused parameter.</param>
        /// <param name="e">Unused parameter.</param>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // FlipView keeps three items in memory at a time, so force the update the size of each so they are resized
            // Other items are not in memory and will be resized when they are loaded
            int startingIndex;

            // Grid_SizeChanged is called on initialization, so make sure that the SelectedIndex is not negative
            if (FlipView.SelectedIndex >= 0)
            {
                // Start with the previous image, if one exists
                if (FlipView.SelectedIndex > 0)
                    startingIndex = FlipView.SelectedIndex - 1;
                else
                    startingIndex = FlipView.SelectedIndex;

                // Update the current image, previous image and next image (the three that are in memory)
                for (int i = 0; i < 3; i++)
                {
                    if (startingIndex + i <= Items.Count)
                    {
                        // Set the Item to itself to trigger a PropertyChangedEvent, causing the image to be resized
                        Items[startingIndex + i].FilePath = Items[startingIndex + i].FilePath;
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the play/pause button according to the current state of the slide show.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="e">Unused parameter.</param>
        private void MainAppBar_Opened(object sender, object e)
        {
            // Hide and show the appropriate Play/Pause button
            TogglePlayPauseButton(_isPaused);
        }
        #endregion

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
            var builder = new BackgroundTaskBuilder();
            builder.Name = Constants.TimerTaskName;
            builder.TaskEntryPoint = Constants.TaskEntryPoint;
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
            var builder = new BackgroundTaskBuilder();
            builder.Name = Constants.UserTaskName;
            builder.TaskEntryPoint = Constants.TaskEntryPoint;
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
            var registration = builder.Register();
        }

        /// <summary>
        /// Helper function to retrieve the app's current Settings instance.
        /// </summary>
        /// <returns>The app's current Settings instance.</returns>
        private Settings GetSettings()
        {
            return ((App)App.Current).Settings;
        }
    }
}
