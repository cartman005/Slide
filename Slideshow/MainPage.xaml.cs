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
        public static Random random;
        private List<StorageFile> fileList;
        private DispatcherTimer timer;
        private ApplicationDataContainer settings = null;
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

            random = new Random();
            timer = new DispatcherTimer();
            Items = new ObservableCollection<ListItem>();
            timer.Tick += Next_Click;
                        
            settings = ApplicationData.Current.RoamingSettings;

            FlipView.ItemsSource = Items;
        }

        private void Next_Click(object sender, object e)
        {
            GetMoreFiles(1);
            FlipView.SelectedIndex++;
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
            fileList.AddRange(await Kozlowski.Slideshow.Background.TileUpdater.GetImageList(KnownFolders.PicturesLibrary)); // Change TileUpdater name   
            GetMoreFiles(10);
            int index;
            if (settings.Values.ContainsKey(Constants.SettingsName))
            {
                index = (int)(settings.Values[Constants.SettingsName]);
            }
            else
            {
                index = Constants.DefaultIntervalIndex;
            }

            ((ComboBox)FindName("Interval")).SelectedIndex = index;

            /* Register background task and create first tile updates */
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                Register_Timer_Task(Constants.IndexList[index]);
                Register_User_Task();
            }

            // Set the input focus to ensure that keyboard events are raised.
            //this.Loaded += delegate { this.Focus(FocusState.Programmatic); };
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private async void GetMoreFiles(int count)
        {
            StorageFile file;
            IRandomAccessStream fileStream;
            BitmapImage bitmapImage;

            for (int i = 0; i < count; i++)
            {
                file = fileList[random.Next(0, fileList.Count)]; // What if count is 0?
                fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);
                Items.Add(new ListItem { Image = bitmapImage, File = file });

                maxIndex++;
            }
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            switch(args.VirtualKey)
            {                
                case VirtualKey.Left:
                //    Previous_Click(null, null);
                    break;
                case VirtualKey.Right:
                    Next_Click(null, null);
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

        private async void Open_File_Click(object sender, RoutedEventArgs e)
        {
            StorageFile file = ((ListItem)FlipView.SelectedItem).File;
            LauncherOptions launcherOptions = new LauncherOptions();
            launcherOptions.DisplayApplicationPicker = true;
            await Launcher.LaunchFileAsync(file, launcherOptions);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        { 
            /* Reset timer */
            timer.Stop();
            timer.Start();

            if (FlipView.SelectedIndex == maxIndex)
            {
                GetMoreFiles(10);
            }
        }
        
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
            if (MainAppBar.Visibility == Visibility.Visible)
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Visible;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Collapsed;
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {            
            timer.Stop();
            if (MainAppBar.Visibility == Visibility.Visible)
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Collapsed;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Visible;
            }
        }

        private void FlipView_SelectionChanged(object sender, object e)
        {
            Next_Click(sender, null);
        }

        private void Interval_Changed(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("Interval changed");
            int index = ((ComboBox)FindName("Interval")).SelectedIndex;

            settings.Values[Constants.SettingsName] = index;

            timer.Interval = TimeSpan.FromSeconds(Constants.IndexList[index]);

            if (!timer.IsEnabled)
                timer.Start();

            Kozlowski.Slideshow.Background.TileUpdater.DoWork(Constants.IndexList[index]);
        }

        private void Register_Timer_Task(int seconds)
        {
            /* Timer Task */
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == Constants.TaskName)
                    return;
            }
            Debug.WriteLine("Register timer task");
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = Constants.TaskName;
            builder.TaskEntryPoint = Constants.TaskEntry;
            builder.SetTrigger(new TimeTrigger(15, false));
            var registration = builder.Register();              
        }

        private void Register_User_Task()
        {
            /* Timer Task */
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == Constants.TaskName)
                    return;
            }
            Debug.WriteLine("Register timer task");
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = "USER_TASK";
            builder.TaskEntryPoint = Constants.TaskEntry;
            builder.SetTrigger(new SystemTrigger(SystemTriggerType.InternetAvailable, false));
            var registration = builder.Register();
        }

        private void MainAppBar_Loaded(object sender, RoutedEventArgs e)
        {
            if (timer.IsEnabled)
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Visible;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Collapsed;
            }
            else
            {
                ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Collapsed;
                ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Visible;
            }
        }
    }

    public class ListItem
    {       
        public BitmapImage Image { get; set; }

        public StorageFile File { get; set; }

        public string Name()
        {
            return File.DisplayName;
        }

        public override string ToString()
        {
            return Name();
        }
    }
}
