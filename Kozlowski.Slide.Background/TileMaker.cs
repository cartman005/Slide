using Kozlowski.Slide.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Notifications;

namespace Kozlowski.Slide.Background
{
    // Uses code from http://code.msdn.microsoft.com/windowsapps/Tile-Update-every-minute-68dbbbff
    public sealed class TileMaker
    {        
        public static IAsyncOperation<IReadOnlyList<StorageFile>> GetImageList(StorageFolder folder, bool includeSubfolders)
        {
            return Task.Run<IReadOnlyList<StorageFile>>(async () =>
            {
                List<String> fileType = new List<String>();
                fileType.Add(".bmp");
                fileType.Add(".gif");
                fileType.Add(".jpg");
                fileType.Add(".jpeg");
                fileType.Add(".png");
                fileType.Add(".tiff");
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileType);

                queryOptions.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
                    
                if (includeSubfolders)
                    queryOptions.FolderDepth = FolderDepth.Deep;
                else
                    queryOptions.FolderDepth = FolderDepth.Shallow;

                // If not shuffled, sort by file name; otherwise, don't bother
                if (!Settings.Instance.Shuffle)
                {
                    queryOptions.SortOrder.Add(new SortEntry
                    {
                        PropertyName = "System.ItemNameDisplay",
                        AscendingOrder = true
                    });
                }
 
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

        public static IAsyncOperation<XmlDocument> CreateTileUpdate(StorageFile file, int tileWidth, int tileHeight)
        {
            return Task.Run<XmlDocument>(async () =>
            {
                try
                {
                    if (file == null)
                        return null;

                    // Create a stream from the file and decode the image
                    using(var fileStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

                        Size dimensions = TileMaker.GetDimensions((int)decoder.PixelWidth, (int)decoder.PixelHeight, tileWidth, tileHeight);

                        // Resize the image to 310x310 and save to Local AppData folder
                        BitmapTransform transform = new BitmapTransform() { ScaledWidth = (uint)dimensions.Width, ScaledHeight = (uint)dimensions.Height };
                        PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                                BitmapPixelFormat.Rgba8,
                                BitmapAlphaMode.Straight,
                                transform,
                                ExifOrientationMode.RespectExifOrientation,
                                ColorManagementMode.DoNotColorManage);

                        var file310x310 = await ApplicationData.Current.LocalFolder.CreateFileAsync(Constants.TileUpdatesFolder + "\\" + file.DisplayName + file.FileType, CreationCollisionOption.GenerateUniqueName);
                        var destinationStream = await file310x310.OpenAsync(FileAccessMode.ReadWrite);

                        BitmapEncoder encoder;
                        switch (Path.GetExtension(file310x310.Path).ToLower())
                        {
                            case ".bmp":
                                encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, destinationStream);
                                break;

                            case ".gif":
                                encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, destinationStream);
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

                        encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, (uint)dimensions.Width, (uint)dimensions.Height, 96, 96, pixelData.DetachPixelData());
                        await encoder.FlushAsync();
                        destinationStream.Dispose();

                        // Create the update
                        // Parent, 150x150 tile
                        var tile1 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Image);
                        var tileImageAttributes = (XmlElement)tile1.GetElementsByTagName("image").Item(0);
                        tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}", Constants.TileUpdatesFolder, file310x310.Name));
                        tileImageAttributes.SetAttribute("alt", file.DisplayName);
                        var bindingElement = (XmlElement)tile1.GetElementsByTagName("binding").Item(0);
                        bindingElement.SetAttribute("branding", "none");

                        // 310x150 tile
                        var tile2 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Image);
                        tileImageAttributes = (XmlElement)tile2.GetElementsByTagName("image").Item(0);
                        tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}", Constants.TileUpdatesFolder, file310x310.Name));
                        tileImageAttributes.SetAttribute("alt", file.DisplayName);
                        bindingElement = (XmlElement)tile2.GetElementsByTagName("binding").Item(0);
                        bindingElement.SetAttribute("branding", "none");

                        // Append to parent tile
                        IXmlNode node = tile1.ImportNode(tile2.GetElementsByTagName("binding").Item(0), true);
                        tile1.GetElementsByTagName("visual").Item(0).AppendChild(node);

                        // 310x310 tile
                        var tile3 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310Image);
                        tileImageAttributes = (XmlElement)tile3.GetElementsByTagName("image").Item(0);
                        tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}", Constants.TileUpdatesFolder, file310x310.Name));
                        tileImageAttributes.SetAttribute("alt", file.DisplayName);
                        bindingElement = (XmlElement)tile3.GetElementsByTagName("binding").Item(0);
                        bindingElement.SetAttribute("branding", "none");

                        // Append to parent tile
                        node = tile1.ImportNode(bindingElement, true);
                        tile1.GetElementsByTagName("visual").Item(0).AppendChild(node);
                    
                        return tile1;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("{0} Error", ex);
                }

                return null;
            }).AsAsyncOperation<XmlDocument>();
        }

        public static IAsyncAction GenerateTiles(int seconds, IReadOnlyList<StorageFile> IFileList)
        {
            return Task.Run(async () =>
            {
                // TODO Should the settings be created here?
                Settings settings = Settings.Instance;

                var updater = TileUpdateManager.CreateTileUpdaterForApplication();
                updater.EnableNotificationQueue(true);
                updater.Clear();

                // Clear existing images
                try
                {
                    StorageFolder tileUpdateFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(Constants.TileUpdatesFolder);
                    await tileUpdateFolder.DeleteAsync();
                }
                catch(FileNotFoundException ex)
                {
                    Debug.WriteLine("Tile updates folder not found {0}", ex.Source);
                }

                DateTime now = DateTime.Now;
                DateTime planTill = now.AddMinutes(30);
                DateTime updateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);

                List<StorageFile> fileList = new List<StorageFile>();
                fileList.AddRange(IFileList);
                
                // Create the first tile
                int index;
                StorageFile file;

                if (settings.Shuffle)
                    index = SingleRandom.Instance.Next(0, fileList.Count);
                else
                    index = 0;
                
                if (fileList.Count >= index + 1)
                {                
                    file = fileList[index];
                    fileList.RemoveAt(index);

                    var tile = await CreateTileUpdate(file, 310, 310);
                    if (tile != null)
                    {
                        updater.Update(new TileNotification(tile) { ExpirationTime = now.AddMinutes(1) });
                    }

                    // Create the rest of the tiles
                    Debug.WriteLine("Create updates from " + updateTime + " to " + planTill);
                    for (var startPlanning = updateTime; startPlanning < planTill; startPlanning = startPlanning.AddSeconds(seconds))
                    {                        
                            if (fileList.Count < 1)
                                fileList.AddRange(await GetImageList(settings.RootFolder, settings.IncludeSubfolders));

                            if (settings.Shuffle)
                                index = SingleRandom.Instance.Next(0, fileList.Count);
                            else
                            {
                                index++;

                                if (fileList.Count <= index)
                                    index = 0;
                            }

                            if (fileList.Count >= index + 1)
                            {
                                file = fileList[index];
                                fileList.RemoveAt(index);
                                Debug.WriteLine("Schedule at " + startPlanning);
                                tile = await CreateTileUpdate(file, 310, 310);
                                if (tile != null)
                                {
                                    ScheduledTileNotification scheduledNotification = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning)) { ExpirationTime = startPlanning.AddMinutes(1) };
                                    updater.AddToSchedule(scheduledNotification);
                                }
                            }
                            else
                            {
                                Debug.WriteLine("No images found");
                                return;
                                // TODO This should be handled
                            }
                    }
                }
            }).AsAsyncAction();
        }

        public static Size GetDimensions(int originalWidth, int originalHeight, int targetWidth, int targetHeight)
        {
            Size newDimensions = new Size();

            if (originalWidth >= targetWidth && originalHeight >= targetHeight)
            {
                // Resize the image
                double ratio = (double)originalHeight / originalWidth;

                // Landscape
                if (ratio <= 1)
                {
                    newDimensions.Width = targetWidth;
                    newDimensions.Height = (int)(targetWidth * ratio);
                }
                // Portrait
                else
                {
                    newDimensions.Width = (int)(targetHeight * (1 / ratio));
                    newDimensions.Height = targetHeight;
                }
            }
            else
            {
                // Don't resize
                newDimensions.Width = originalWidth;
                newDimensions.Height = originalHeight;
            }
            return newDimensions;
        }
    }
}