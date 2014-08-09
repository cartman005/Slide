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

            settings = new Settings();

            Items = new ObservableCollection<ListItem>();
            FlipView.ItemsSource = Items;

            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
            isPaused = false;
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
            // Reset the timer
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
            fileList = new List<StorageFile>();
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
            LoadMoreFiles(10);

            //((ComboBox)FindName("Interval")).SelectedIndexn = index;
            Debug.WriteLine("Setting " + settings.Interval);
            settings.PropertyChanged += x_PropertyChanged;
            timer.Interval = TimeSpan.FromSeconds(settings.Interval);

            /* Register background task and create first tile updates */
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
        }

        private async void x_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            timer.Interval = TimeSpan.FromSeconds(settings.Interval);

            if (!timer.IsEnabled)
                timer.Start();

            await TileMaker.CreateTiles(settings.Interval);
        }

        private void LoadMoreFiles(int count)
        {
            StorageFile file;

            for (int i = 0; i < count; i++)
            {
                file = fileList[SingleRandom.Instance.Next(0, fileList.Count)]; // What if count is 0?
                Items.Add(new ListItem { File = file });
                maxIndex++;
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
