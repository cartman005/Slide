using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Notifications;

namespace BackgroundSlideshow
{
    public sealed class TileUpdater : IBackgroundTask
    {
        private ApplicationDataContainer settings = null;
        private IReadOnlyList<StorageFile> fileList;

        public TileUpdater()
        {
            settings = ApplicationData.Current.RoamingSettings;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {            
            var defferal = taskInstance.GetDeferral();
            Debug.WriteLine("Background task running");
            int index;
            settings = ApplicationData.Current.RoamingSettings;
            if (settings.Values.ContainsKey(Constants.SettingsName))
            {
                index = (int)(settings.Values[Constants.SettingsName]);
            }
            else
            {
                index = Constants.DefaultIntervalIndex;
            }

            Debug.WriteLine("Using index " + index);

            fileList = await GetImageList(KnownFolders.PicturesLibrary);

            await Run(Constants.IndexList[index], fileList);
            Debug.WriteLine("Background task done");
            defferal.Complete();
        }

        public static IAsyncOperation<IReadOnlyList<StorageFile>> GetImageList(StorageFolder folder)
        {
            return Task.Run<IReadOnlyList<StorageFile>>(async () =>
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
                }).AsAsyncOperation<IReadOnlyList<StorageFile>>();
        }

        //private async static Task<IReadOnlyList<StorageFile>> GetImageList(StorageFolder folder)
        //{
        //    List<String> fileType = new List<String>();
        //    fileType.Add(".bmp");
        //    fileType.Add(".jpg");
        //    fileType.Add(".jpeg");
        //    fileType.Add(".png");
        //    fileType.Add(".tiff");
        //    var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileType);
        //    queryOptions.FolderDepth = FolderDepth.Deep;

        //    var query = folder.CreateFileQueryWithOptions(queryOptions);

        //    var fileList = await query.GetFilesAsync();
        //    if (fileList.Count < 1)
        //    {
        //        Debug.WriteLine("No pictures found");
        //    }
        //    else
        //    {
        //        Debug.WriteLine(fileList.Count + " pictures found");
        //    }

        //    return fileList;
        //}

        public static IAsyncOperation<XmlDocument> CreateUpdate(StorageFile file)
        {
            return Task.Run<XmlDocument>(async () =>
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
            }).AsAsyncOperation<XmlDocument>();
        }

        //private async Task<XmlDocument> CreateUpdate(StorageFile file)
        //{
        //    try
        //    {
        //        if (file == null)
        //            return null;

        //        Debug.WriteLine("Got file " + file.Name);

        //        // create a stream from the file and decode the image
        //        var fileStream = await file.OpenAsync(FileAccessMode.Read);
        //        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
        //        Debug.WriteLine("Decoded");
        //        uint height310x310, width310x310;
        //        double ratio;

        //        Debug.WriteLine("Original Width = " + decoder.PixelWidth);
        //        Debug.WriteLine("Original Height = " + decoder.PixelHeight);

        //        ratio = (double)decoder.PixelHeight / decoder.PixelWidth;

        //        /* Landscape */
        //        if (ratio <= 1)
        //        {
        //            if (decoder.PixelWidth < 310)
        //            {
        //                Debug.WriteLine(file.Name + " is too small");
        //                return null;
        //            }

        //            height310x310 = (uint)(310 * ratio);
        //            width310x310 = 310;

        //        }
        //        /* Portrait */
        //        else
        //        {
        //            if (decoder.PixelHeight < 310)
        //            {
        //                Debug.WriteLine(file.Name + " is too small");
        //                return null;
        //            }

        //            width310x310 = (uint)(310 * (1 / ratio));
        //            height310x310 = 310;
        //        }

        //        /* 310 x 310 */
        //        BitmapTransform transform = new BitmapTransform() { ScaledHeight = height310x310, ScaledWidth = width310x310 };
        //        PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
        //                BitmapPixelFormat.Rgba8,
        //                BitmapAlphaMode.Straight,
        //                transform,
        //                ExifOrientationMode.RespectExifOrientation,
        //                ColorManagementMode.DoNotColorManage);

        //        var file310x310 = await ApplicationData.Current.LocalFolder.CreateFileAsync(file.DisplayName + file.FileType, CreationCollisionOption.GenerateUniqueName);
        //        var destinationStream = await file310x310.OpenAsync(FileAccessMode.ReadWrite);

        //        BitmapEncoder encoder;
        //        switch (Path.GetExtension(file310x310.Path).ToLower())
        //        {
        //            case ".bmp":
        //                encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, destinationStream);
        //                break;

        //            case ".jpg":
        //            case ".jpeg":
        //                encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, destinationStream);
        //                break;

        //            case ".png":
        //                encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, destinationStream);
        //                break;

        //            case ".tiff":
        //                encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.TiffEncoderId, destinationStream);
        //                break;

        //            default:
        //                return null;
        //        }

        //        encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, width310x310, height310x310, 96, 96, pixelData.DetachPixelData());
        //        await encoder.FlushAsync();
        //        destinationStream.Dispose();

        //        Debug.WriteLine(file310x310.Path);

        //        /* Set tile update */
        //        var tile1 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Image);
        //        var tileImageAttributes = (XmlElement)tile1.GetElementsByTagName("image").Item(0);
        //        tileImageAttributes.SetAttribute("src", "ms-appdata:///local/" + file310x310.Name);
        //        tileImageAttributes.SetAttribute("alt", file.DisplayName);
        //        var bindingElement = (XmlElement)tile1.GetElementsByTagName("binding").Item(0);
        //        bindingElement.SetAttribute("branding", "none");

        //        var tile2 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Image);
        //        tileImageAttributes = (XmlElement)tile2.GetElementsByTagName("image").Item(0);
        //        tileImageAttributes.SetAttribute("src", "ms-appdata:///local/" + file310x310.Name);
        //        tileImageAttributes.SetAttribute("alt", file.DisplayName);
        //        bindingElement = (XmlElement)tile2.GetElementsByTagName("binding").Item(0);
        //        bindingElement.SetAttribute("branding", "none");

        //        IXmlNode node = tile1.ImportNode(tile2.GetElementsByTagName("binding").Item(0), true);
        //        tile1.GetElementsByTagName("visual").Item(0).AppendChild(node);

        //        var tile3 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310Image);
        //        tileImageAttributes = (XmlElement)tile3.GetElementsByTagName("image").Item(0);
        //        tileImageAttributes.SetAttribute("src", "ms-appdata:///local/" + file310x310.Name);
        //        tileImageAttributes.SetAttribute("alt", file.DisplayName);
        //        bindingElement = (XmlElement)tile3.GetElementsByTagName("binding").Item(0);
        //        bindingElement.SetAttribute("branding", "none");

        //        node = tile1.ImportNode(bindingElement, true);
        //        tile1.GetElementsByTagName("visual").Item(0).AppendChild(node);

        //        Debug.WriteLine("Done");

        //        return tile1;
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.WriteLine("{0} Error ", e);
        //        Debug.WriteLine(e.StackTrace);
        //    }

        //    return null;
        //}

        private async Task Run(int seconds, IReadOnlyList<StorageFile> fileList)
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
