using Kozlowski.Slideshow.Background;
using Kozlowski.Slideshow.Common;
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
using Kozlowski.Slideshow.Shared;
using System.ComponentModel;
using Windows.UI.Popups;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Kozlowski.Slideshow
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

        public void Move_Forward()
        {
            if (maxIndex - FlipView.SelectedIndex < 3)
            {
                LoadMoreFiles(10);
            }
        }

        public void Reset_Timer()
        {
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
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
            isPaused = false; // May be changed by LoadMoreFiles so shouldn't be changed after
            LoadMoreFiles(10);

            timer.Interval = TimeSpan.FromSeconds(settings.Interval);

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

            /* Create first tile updates */
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
                LoadMoreFiles(10);
                Debug.WriteLine("The selected index is now "+ FlipView.SelectedIndex);
            }
            
            await TileMaker.CreateTiles(settings.Interval, fileList);
        }

        private async void LoadMoreFiles(int count)
        {
            int index;
            StorageFile file;

            SingleRandom random = SingleRandom.Instance;

            for (int i = 0; i < count; i++)
            {
                if (fileList.Count < count)
                    fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));

                index = random.Next(0, fileList.Count);

                if (fileList.Count >= index + 1)
                {
                    file = fileList[index];
                    Items.Add(new ListItem { File = file });
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
        }

        private void CommandInvokedHandler(IUICommand command)
        {
            switch(command.Label)
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
            switch(args.VirtualKey)
            {                
                case VirtualKey.Left:
                    Reset_Timer();
                    break;
                case VirtualKey.Right:
                    Reset_Timer();
                    Move_Forward();
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
            Reset_Timer();

            if (((ListItem)FlipView.SelectedItem) != null)
                FileName.Text = ((ListItem)FlipView.SelectedItem).Name;

            if (maxIndex - FlipView.SelectedIndex < 3)
                LoadMoreFiles(10);
        }

        private void Timer_Tick(object sender, object e)
        {
            FlipView.SelectedIndex++;
            Move_Forward();
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
            /* Timer Task */
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
    }
}
