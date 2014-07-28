using BackgroundSlideshow;
using Slideshow.Common;
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

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Slideshow
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private LinkedList<StorageFile> imageList;
        private LinkedListNode<StorageFile> node;
        public static Random random;
        private IReadOnlyList<StorageFile> fileList;
        private DispatcherTimer timer;
        private ApplicationDataContainer settings = null;

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
            timer.Tick += Next_Click;
                        
            settings = ApplicationData.Current.RoamingSettings;
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
            fileList = await GetImageList(KnownFolders.PicturesLibrary);
            StorageFile file = fileList[random.Next(0, fileList.Count)];

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

            /* Set image */
            imageList = new LinkedList<StorageFile>();
            if (file != null)
            {
                IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);
                MainImage.Source = bitmapImage;
                imageList.AddFirst(file);
                node = imageList.First;
                ((TextBlock)FindName("FileName")).Text = file.DisplayName;
            }

            /* Register background task and create first tile updates */
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                Register_Timer_Task(Constants.IndexList[index]);
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

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (node != null)
            { 
                if (node == imageList.Last)
                {
                    var file = fileList[random.Next(0, fileList.Count)];
                    imageList.AddAfter(node, file);

                    if (imageList.Count > Constants.ListLimit)
                    {
                        imageList.RemoveFirst();
                    }
                }

                node = node.Next;

                IRandomAccessStream fileStream = await node.Value.OpenAsync(Windows.Storage.FileAccessMode.Read);
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);
                MainImage.Source = bitmapImage;
                ((TextBlock)FindName("FileName")).Text = node.Value.DisplayName;

                /* Reset timer */
                timer.Stop();
                timer.Start();
            }
        }

        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (node != null)
            {
                if (node == imageList.First)
                {
                    var file = fileList[random.Next(0, fileList.Count)];
                    imageList.AddBefore(node, file);

                    if (imageList.Count > Constants.ListLimit)
                    {
                        imageList.RemoveLast();
                    }
                }

                node = node.Previous;

                IRandomAccessStream fileStream = await node.Value.OpenAsync(Windows.Storage.FileAccessMode.Read);
                BitmapImage bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(fileStream);
                MainImage.Source = bitmapImage;
                ((TextBlock)MainAppBar.FindName("FileName")).Text = node.Value.DisplayName;

                /* Reset timer */
                timer.Stop();
                timer.Start();
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Collapsed;
            ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Visible;
            timer.Start();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            ((Button)MainAppBar.FindName("PauseButton")).Visibility = Visibility.Collapsed;
            ((Button)MainAppBar.FindName("PlayButton")).Visibility = Visibility.Visible;
            timer.Stop();
        }

        private void Next_Click(object sender, object e)
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

            Run(Constants.IndexList[index], fileList);
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

        private async static Task<IReadOnlyList<StorageFile>> GetImageList(StorageFolder folder)
        {
            List<String> fileType = new List<String>();
            fileType.Add(".bmp");
            fileType.Add(".jpg");
            fileType.Add(".jpeg");
            fileType.Add(".png");
            fileType.Add(".tiff");
            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileType);
            queryOptions.FolderDepth = FolderDepth.Deep;

            var query = folder.CreateFileQueryWithOptions(queryOptions);

            var fileList = await query.GetFilesAsync();
            if (fileList.Count < 1)
            {
                Debug.WriteLine("No pictures found");
            }
            else
            {
                Debug.WriteLine(fileList.Count + " pictures found");
            }

            return fileList;
        }

        private async Task<XmlDocument> CreateUpdate(StorageFile file)
        {
            try
            {
                if (file == null)
                    return null;

                Debug.WriteLine("Got file " + file.Name);

                // create a stream from the file and decode the image
                var fileStream = await file.OpenAsync(FileAccessMode.Read);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                Debug.WriteLine("Decoded");
                uint height310x310, width310x310;
                double ratio;

                Debug.WriteLine("Original Width = " + decoder.PixelWidth);
                Debug.WriteLine("Original Height = " + decoder.PixelHeight);

                ratio = (double)decoder.PixelHeight / decoder.PixelWidth;

                /* Landscape */
                if (ratio <= 1)
                {
                    if (decoder.PixelWidth < 310)
                    {
                        Debug.WriteLine(file.Name + " is too small");
                        return null;
                    }

                    height310x310 = (uint)(310 * ratio);
                    width310x310 = 310;

                }
                /* Portrait */
                else
                {
                    if (decoder.PixelHeight < 310)
                    {
                        Debug.WriteLine(file.Name + " is too small");
                        return null;
                    }

                    width310x310 = (uint)(310 * (1 / ratio));
                    height310x310 = 310;
                }

                /* 310 x 310 */
                BitmapTransform transform = new BitmapTransform() { ScaledHeight = height310x310, ScaledWidth = width310x310 };
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Rgba8,
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.DoNotColorManage);

                var file310x310 = await ApplicationData.Current.LocalFolder.CreateFileAsync(file.DisplayName + file.FileType, CreationCollisionOption.GenerateUniqueName);
                var destinationStream = await file310x310.OpenAsync(FileAccessMode.ReadWrite);

                BitmapEncoder encoder;
                switch (Path.GetExtension(file310x310.Path).ToLower())
                {
                    case ".bmp":
                        encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, destinationStream);
                        break;

                    case ".jpg":
                    case ".jpeg":
                        encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, destinationStream);
                        break;

                    case ".png":
                        encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, destinationStream);
                        break;

                    case ".tiff":
                        encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.TiffEncoderId, destinationStream);
                        break;

                    default:
                        return null;
                }

                encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, width310x310, height310x310, 96, 96, pixelData.DetachPixelData());
                await encoder.FlushAsync();
                destinationStream.Dispose();

                Debug.WriteLine(file310x310.Path);

                /* Set tile update */
                var tile1 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Image);
                var tileImageAttributes = (XmlElement)tile1.GetElementsByTagName("image").Item(0);
                tileImageAttributes.SetAttribute("src", "ms-appdata:///local/" + file310x310.Name);
                tileImageAttributes.SetAttribute("alt", file.DisplayName);
                var bindingElement = (XmlElement)tile1.GetElementsByTagName("binding").Item(0);
                bindingElement.SetAttribute("branding", "none");

                var tile2 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Image);
                tileImageAttributes = (XmlElement)tile2.GetElementsByTagName("image").Item(0);
                tileImageAttributes.SetAttribute("src", "ms-appdata:///local/" + file310x310.Name);
                tileImageAttributes.SetAttribute("alt", file.DisplayName);
                bindingElement = (XmlElement)tile2.GetElementsByTagName("binding").Item(0);
                bindingElement.SetAttribute("branding", "none");

                IXmlNode node = tile1.ImportNode(tile2.GetElementsByTagName("binding").Item(0), true);
                tile1.GetElementsByTagName("visual").Item(0).AppendChild(node);

                var tile3 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310Image);
                tileImageAttributes = (XmlElement)tile3.GetElementsByTagName("image").Item(0);
                tileImageAttributes.SetAttribute("src", "ms-appdata:///local/" + file310x310.Name);
                tileImageAttributes.SetAttribute("alt", file.DisplayName);
                bindingElement = (XmlElement)tile3.GetElementsByTagName("binding").Item(0);
                bindingElement.SetAttribute("branding", "none");

                node = tile1.ImportNode(bindingElement, true);
                tile1.GetElementsByTagName("visual").Item(0).AppendChild(node);

                Debug.WriteLine("Done");

                return tile1;
            }
            catch (Exception e)
            {
                Debug.WriteLine("{0} Error ", e);
                Debug.WriteLine(e.StackTrace);
            }

            return null;
        }

        private async void Run(int seconds, IReadOnlyList<StorageFile> fileList)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();

            await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);

            DateTime now = DateTime.Now;
            DateTime planTill = now.AddMinutes(30);
            DateTime updateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);

            var random = new Random(); // this should be placed in a static member variable, but is ok for this example

            /* First background tile */
            StorageFile file;
            file = fileList[random.Next(0, fileList.Count)];
            var tile = await CreateUpdate(file);
            if (tile != null)
            {
                updater.Update(new TileNotification(tile) { ExpirationTime = now.AddMinutes(1) });
            }

            for (var startPlanning = updateTime; startPlanning < planTill; startPlanning = startPlanning.AddSeconds(seconds))
            {
                Debug.WriteLine(startPlanning);
                Debug.WriteLine(planTill);

                try
                {
                    file = fileList[random.Next(0, fileList.Count)];

                    tile = await CreateUpdate(file);
                    if (tile != null)
                    {
                        ScheduledTileNotification scheduledNotification = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning)) { ExpirationTime = startPlanning.AddMinutes(1) };
                        updater.AddToSchedule(scheduledNotification);

                        Debug.WriteLine("schedule for: " + startPlanning);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("exception: " + e.Message);
                }
            }
        }
    }
}
