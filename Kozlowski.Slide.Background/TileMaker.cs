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
    /// <summary>
    /// This class is responsible for creating tiles for the Start screen and scheduling the tile updates.
    /// Its functions are defined staticly to provide shared access to them from both the background task and the main app page.
    /// </summary>
    public sealed class TileMaker
    {        
        /// <summary>
        /// Gets the 
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="includeSubfolders"></param>
        /// <param name="shuffle"></param>
        /// <returns>A list of the image files to be used in the slideshow.</returns>
        public static IAsyncOperation<IReadOnlyList<StorageFile>> GetImageList(StorageFolder folder, bool includeSubfolders, bool shuffle)
        {
            return Task.Run<IReadOnlyList<StorageFile>>(async () =>
            {
                var fileTypes = new List<String>();
                fileTypes.Add(".bmp");
                fileTypes.Add(".gif");
                fileTypes.Add(".jpg");
                fileTypes.Add(".jpeg");
                fileTypes.Add(".png");
                fileTypes.Add(".tiff");
                var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypes);

                queryOptions.IndexerOption = IndexerOption.UseIndexerWhenAvailable;
                    
                if (includeSubfolders)
                    queryOptions.FolderDepth = FolderDepth.Deep;
                else
                    queryOptions.FolderDepth = FolderDepth.Shallow;

                // If not shuffled, sort by file name; otherwise, don't bother
                if (!shuffle)
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
                    Debug.WriteLine("{0} pictures found", fileList.Count);
                }

                return fileList;

            }).AsAsyncOperation<IReadOnlyList<StorageFile>>();
        }

        public static IAsyncOperation<XmlDocument> CreateTileUpdate(string destination, StorageFile file, int tileWidth, int tileHeight)
        {
            return Task.Run<XmlDocument>(async () =>
            {
                try
                {
                    if (file == null)
                        return null;

                    // Create a stream from the file and decode the image
                    using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        var decoder = await BitmapDecoder.CreateAsync(fileStream);
                        var dimensions = TileMaker.GetDimensions((int)decoder.PixelWidth, (int)decoder.PixelHeight, tileWidth, tileHeight);

                        // Resize the image to 310x310 and save to Local AppData folder
                        var transform = new BitmapTransform() { ScaledWidth = (uint)dimensions.Width, ScaledHeight = (uint)dimensions.Height };
                        var pixelData = await decoder.GetPixelDataAsync(
                                BitmapPixelFormat.Rgba8,
                                BitmapAlphaMode.Straight,
                                transform,
                                ExifOrientationMode.RespectExifOrientation,
                                ColorManagementMode.DoNotColorManage);

                        var cachedFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(string.Format("{0}\\{1}\\{2}{3}", destination, Constants.TileUpdatesFolder, file.DisplayName, file.FileType), CreationCollisionOption.GenerateUniqueName);
                        var destinationStream = await cachedFile.OpenAsync(FileAccessMode.ReadWrite);

                        // Find the correct encoder for the image file
                        BitmapEncoder encoder;
                        switch (Path.GetExtension(cachedFile.Path).ToLower())
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

                        // Create the update
                        // Parent, 150x150 tile
                        var parentTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Image);
                        var tileImageAttributes = (XmlElement)parentTile.GetElementsByTagName("image").Item(0);
                        tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}/{2}", destination, Constants.TileUpdatesFolder, cachedFile.Name));
                        tileImageAttributes.SetAttribute("alt", file.DisplayName);
                        var bindingElement = (XmlElement)parentTile.GetElementsByTagName("binding").Item(0);
                        bindingElement.SetAttribute("branding", "none");

                        // 310x150 tile
                        var wideTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Image);
                        tileImageAttributes = (XmlElement)wideTile.GetElementsByTagName("image").Item(0);
                        tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}/{2}", destination, Constants.TileUpdatesFolder, cachedFile.Name));
                        tileImageAttributes.SetAttribute("alt", file.DisplayName);
                        bindingElement = (XmlElement)wideTile.GetElementsByTagName("binding").Item(0);
                        bindingElement.SetAttribute("branding", "none");

                        // Append to parent tile
                        IXmlNode node = parentTile.ImportNode(wideTile.GetElementsByTagName("binding").Item(0), true);
                        parentTile.GetElementsByTagName("visual").Item(0).AppendChild(node);

                        // 310x310 tile
                        var largeTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310Image);
                        tileImageAttributes = (XmlElement)largeTile.GetElementsByTagName("image").Item(0);
                        tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}/{2}", destination, Constants.TileUpdatesFolder, cachedFile.Name));
                        tileImageAttributes.SetAttribute("alt", file.DisplayName);
                        bindingElement = (XmlElement)largeTile.GetElementsByTagName("binding").Item(0);
                        bindingElement.SetAttribute("branding", "none");

                        // Append to parent tile
                        node = parentTile.ImportNode(bindingElement, true);
                        parentTile.GetElementsByTagName("visual").Item(0).AppendChild(node);
                    
                        return parentTile;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("An error occurred: '{0}'", ex);
                }

                return null;
            }).AsAsyncOperation<XmlDocument>();
        }

        /// <summary>
        /// This method uses code adapted from an MSDN sample posted by Dave Smits on 12/03/2012 here:
        /// http://code.msdn.microsoft.com/windowsapps/Tile-Update-every-minute-68dbbbff
        /// </summary>
        /// <param name="tileId">The ID of the tile to create updates for. Will be null for the main app tile.</param>
        /// <param name="seconds"></param>
        /// <param name="IFileList"></param>
        /// <param name="sourceFolder"></param>
        /// <param name="includeSubfolders"></param>
        /// <param name="shuffle"></param>
        /// <returns></returns>
        public static IAsyncAction GenerateTiles(string subfolder, string tileId, int seconds, StorageFolder sourceFolder, bool includeSubfolders, bool shuffle)
        {
            return Task.Run(async () =>
            {
                TileUpdater updater;

                if (tileId == "")
                    updater = TileUpdateManager.CreateTileUpdaterForApplication();
                else
                    updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tileId);

                updater.EnableNotificationQueue(true);
                updater.Clear();

                // Clear existing images by deleting the specifed Tile Updates subfolder in AppData
                try
                {
                    var parentFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(subfolder);
                    var tileUpdatesFolder = await parentFolder.GetFolderAsync(Constants.TileUpdatesFolder);
                    await tileUpdatesFolder.DeleteAsync();
                }
                catch (FileNotFoundException ex)
                {
                    Debug.WriteLine("Tile updates folder not found: '{0}'", ex);
                }

                var now = DateTime.Now;
                var planTill = now.AddMinutes(30);
                var updateTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);

                var fileList = new List<StorageFile>();
                fileList.AddRange(await GetImageList(sourceFolder, includeSubfolders, shuffle));

                // Create the first tile
                int index;
                StorageFile file;

                if (shuffle)
                    index = SingleRandom.Instance.Next(0, fileList.Count);
                else
                    index = 0;
                
                if (fileList.Count >= index + 1)
                {                
                    file = fileList[index];
                    fileList.RemoveAt(index);

                    var tile = await CreateTileUpdate(subfolder, file, 310, 310);
                    if (tile != null)
                    {
                        updater.Update(new TileNotification(tile) { ExpirationTime = now.AddMinutes(1) });
                    }

                    // Create the rest of the tiles
                    Debug.WriteLine("Create updates from {0} to {1}", updateTime, planTill);
                    for (var startPlanning = updateTime; startPlanning < planTill; startPlanning = startPlanning.AddSeconds(seconds))
                    {                        
                            if (fileList.Count < 1)
                                fileList.AddRange(await GetImageList(sourceFolder, includeSubfolders, shuffle));

                            if (shuffle)
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
                                Debug.WriteLine("Schedule {0} at {1}", file.DisplayName, startPlanning);
                                tile = await CreateTileUpdate(subfolder, file, 310, 310);
                                if (tile != null)
                                {
                                    var scheduledNotification = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning)) { ExpirationTime = startPlanning.AddMinutes(1) };
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
            var newDimensions = new Size();

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