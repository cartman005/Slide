using Kozlowski.Slide.Background;
using Kozlowski.Slide.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Graphics.Imaging;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Windows.System;
using System.Collections.ObjectModel;
using Kozlowski.Slide.Shared;
using System.ComponentModel;
using Windows.UI.Popups;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Kozlowski.Slide
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private List<StorageFile> fileList;
        private DispatcherTimer timer;
        private Settings settings;
        private bool isPaused;
        public ObservableCollection<ListItem> Items { get; set; }
        private int maxIndex;

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;

            settings = Settings.Instance;
            settings.PropertyChanged += Settings_Changed;

            Items = new ObservableCollection<ListItem>();
            FlipView.ItemsSource = Items;

            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            isPaused = true;

            fileList = new List<StorageFile>();
        }

        public async void Move_Forward()
        {
            if (maxIndex > 0)
            {
                if (maxIndex - FlipView.SelectedIndex < 3)
                {
                    // TODO Should this always be awaited?
                    await LoadMoreFiles(10);
                }
            }
        }

        public void ResetTimer()
        {
            Debug.WriteLine("Reset Timer");
            timer.Stop();
            timer.Start();
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            /* Set up timer first in case it gets started */
            timer.Interval = TimeSpan.FromSeconds(settings.Interval);
            Debug.WriteLine("Timer set to " + settings.Interval);

            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
            isPaused = false; // Should be set before calling LoadMoreFiles
            await LoadMoreFiles(10);

            /* Register background task */
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                RegisterTimerTask(settings.Interval);
                RegisterUserTask();
            }

            // Set the input focus to ensure that keyboard events are raised.
            //this.Loaded += delegate { this.Focus(FocusState.Programmatic); };
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            /* Create first set of tile updates */
            Debug.WriteLine("Create updates");
            await TileMaker.CreateTiles(settings.Interval, fileList);
        }

        private async void Settings_Changed(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Settings Changed");
            timer.Interval = TimeSpan.FromSeconds(settings.Interval);

            if (!timer.IsEnabled)
                timer.Start();

            /* Change collection of images to use */
            if (e.PropertyName == "FolderPath" || e.PropertyName == "IncludeSubfolders")
            {
                Debug.WriteLine("The selected index was " + FlipView.SelectedIndex);
                Items.Clear();
                maxIndex = 0;
                fileList.Clear();
                fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
                await LoadMoreFiles(10);
                Debug.WriteLine("The selected index is now " + FlipView.SelectedIndex);
            }

            await TileMaker.CreateTiles(settings.Interval, fileList);
        }

        private async Task LoadMoreFiles(int count)
        {
            int index;

            for (int i = 0; i < count; i++)
            {
                if (fileList.Count < count)
                    fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));

                if (settings.Shuffle)
                    index = SingleRandom.Instance.Next(0, fileList.Count);
                else
                {
                    /*
                    if (maxIndex + 1 >= fileList.Count)
                        index = (maxIndex + 1) % fileList.Count;
                    else
                        index = maxIndex;
                     */
                    index = 0;
                }

                if (fileList.Count >= index + 1)
                {
                    Items.Add(new ListItem { File = fileList[index] });
                    fileList.RemoveAt(index);
                    maxIndex++;
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

            return;
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            switch (command.Label)
            {
                case "Try again":
                    Move_Forward();
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
                    //Move_Forward();
                    break;
                case VirtualKey.Space:
                    if (timer.IsEnabled)
                        Pause_Click(null, null);
                    else
                        Play_Click(null, null);
                    break;
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {

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
            StorageFile file = await StorageFile.GetFileFromPathAsync(((ListItem)FlipView.SelectedItem).Path);
            LauncherOptions launcherOptions = new LauncherOptions();
            launcherOptions.DisplayApplicationPicker = true;
            await Launcher.LaunchFileAsync(file, launcherOptions);
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
            Debug.WriteLine("Selection changed");
            ResetTimer();

            if (((ListItem)FlipView.SelectedItem) != null)
                FileName.Text = ((ListItem)FlipView.SelectedItem).Name;

            /* Load more files if necessary */
            Move_Forward();
        }

        private void Timer_Tick(object sender, object e)
        {
            Debug.WriteLine("Timer tick");
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

        private void RegisterTimerTask(int seconds)
        {
            /* Timer Task */
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == Constants.TimerTaskName)
                    return;
            }
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = Constants.TimerTaskName;
            builder.TaskEntryPoint = Constants.TaskEntry;
            builder.SetTrigger(new TimeTrigger(15, false));
            var registration = builder.Register();
        }

        private void RegisterUserTask()
        {
            /* User Present Task */
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == Constants.UserTaskName)
                    return;
            }
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = Constants.UserTaskName;
            builder.TaskEntryPoint = Constants.TaskEntry;
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
            var registration = builder.Register();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            /* FlipView keeps three items in memory at a time, so force the update the size of each of them */
            int startingIndex;

            if (FlipView.SelectedIndex >= 0)
            {
                if (FlipView.SelectedIndex > 0)
                    startingIndex = FlipView.SelectedIndex - 1;
                else
                    startingIndex = FlipView.SelectedIndex;

                for (int i = 0; i < 3; i++)
                {
                    if (startingIndex + i <= maxIndex)
                    {
                        Debug.WriteLine("Update the size of " + Items[startingIndex + i].Name);
                        Items[startingIndex + i].File = Items[startingIndex + i].File;
                    }
                }
            }
        }
    }
}
