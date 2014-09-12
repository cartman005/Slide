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
using Windows.UI.StartScreen;

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

        /// <summary>
        /// Create a single tile update using the provided arguments.
        /// </summary>
        /// <param name="file">The image file to use for the tile update.</param>
        /// <param name="destinationFolder">The name of the folder in which to store the cached image file for the tile update.</param>
        /// <param name="tileWidth">The width of the cached tile image. This should be the width of the largest tile size.</param>
        /// <param name="tileHeight">The height of the cached tile image. This should be the height of the largest tile size.</param>
        /// <returns></returns>
        private static async Task<XmlDocument> CreateTileUpdate(StorageFile file, string destinationFolder, int tileWidth, int tileHeight)
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

                    var cachedFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(string.Format("{0}\\{1}{2}", destinationFolder, file.DisplayName, file.FileType), CreationCollisionOption.GenerateUniqueName);
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
                    tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}", destinationFolder, cachedFile.Name));
                    tileImageAttributes.SetAttribute("alt", file.DisplayName);
                    var bindingElement = (XmlElement)parentTile.GetElementsByTagName("binding").Item(0);
                    bindingElement.SetAttribute("branding", "none");

                    // 310x150 tile
                    var wideTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Image);
                    tileImageAttributes = (XmlElement)wideTile.GetElementsByTagName("image").Item(0);
                    tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}", destinationFolder, cachedFile.Name));
                    tileImageAttributes.SetAttribute("alt", file.DisplayName);
                    bindingElement = (XmlElement)wideTile.GetElementsByTagName("binding").Item(0);
                    bindingElement.SetAttribute("branding", "none");

                    // Append to parent tile
                    IXmlNode node = parentTile.ImportNode(wideTile.GetElementsByTagName("binding").Item(0), true);
                    parentTile.GetElementsByTagName("visual").Item(0).AppendChild(node);

                    // 310x310 tile
                    var largeTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310Image);
                    tileImageAttributes = (XmlElement)largeTile.GetElementsByTagName("image").Item(0);
                    tileImageAttributes.SetAttribute("src", string.Format("ms-appdata:///Local/{0}/{1}", destinationFolder, cachedFile.Name));
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
        }

        /// <summary>
        /// Generates background tile updates using the given arguments.
        /// This method uses code adapted from an MSDN sample posted by Dave Smits on 12/03/2012 here:
        /// http://code.msdn.microsoft.com/windowsapps/Tile-Update-every-minute-68dbbbff
        /// </summary>
        /// <param name="tileId">The ID of the tile to create updates for. Will be blank for the main app tile.</param>
        /// <param name="sourceFolder">The folder to take images from</param>
        /// <param name="destinationFolder">The name of the folder in which to cache the images for tile updates.</param>
        /// <param name="seconds">The number of seconds between updates.</param>
        /// <param name="includeSubfolders">Whether or not to include subfolders inside of the source folder to find images for tile updates.</param>
        /// <param name="shuffle">Whether or not to shuffle the order of the images used for tile updates.</param>
        /// <param name="clearExisting">Whether or not to clear the existing tile update queue before adding the new tile updates.</param>
        /// <returns>A Task representing the completed tile update creation.</returns>
        private static async Task CreateTileUpdates(string tileId, StorageFolder sourceFolder,  string destinationFolder, int seconds, bool includeSubfolders, bool shuffle, bool clearExisting)
        {
            TileUpdater updater;

            if (tileId == "")
                updater = TileUpdateManager.CreateTileUpdaterForApplication();
            else
                updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tileId);

            updater.EnableNotificationQueue(true);

            if (clearExisting)
                updater.Clear();

            // Clear existing images by deleting the specifed tile's subfolder in AppData
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(destinationFolder);
                await folder.DeleteAsync();
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

                var tile = await CreateTileUpdate( file, destinationFolder, (int)Constants.MaxTileSize.Width, (int)Constants.MaxTileSize.Height);
                if (tile != null)
                {
                    //updater.Update(new TileNotification(tile) { ExpirationTime = now.AddMinutes(1) });
                    updater.Update(new TileNotification(tile) { ExpirationTime = now.AddSeconds((seconds * Constants.ConcurrentTiles) > 60 ? seconds * Constants.ConcurrentTiles : 60) });
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
                        tile = await CreateTileUpdate(file, destinationFolder, (int)Constants.MaxTileSize.Width, (int)Constants.MaxTileSize.Width);
                        if (tile != null)
                        {
                            // Tile shouldn't expire less than 60 seconds after it is scheduled for timing reasons
                            //var scheduledNotification = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning)) { ExpirationTime = startPlanning.AddSeconds((seconds * Constants.ConcurrentTiles) > 60 ? seconds * Constants.ConcurrentTiles : 60) };
                            var scheduledNotification = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning));
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
        }

        /// <summary>
        /// Shell function to generate the tile updates for a given tile by filling in the necessary settings using the given tile's Settings instance.
        /// This function does not check whether or not the given tile exists so this should be done calling the function.
        /// </summary>
        /// <param name="tileNumber">The tile to create updates for, using the settings associated with the tile number.</param>
        public static IAsyncAction CreateTileUpdates(int tileNumber, bool clearExisting)
        {
            return Task.Run(async () =>
            {
                switch (tileNumber)
                {
                    case Constants.TILE_1_NUMBER:
                        Debug.WriteLine("Creating primary tile updates");
                        await CreateTileUpdates("", Tile1Settings.Instance.RootFolder, Constants.Tile1SaveFolder, Tile1Settings.Instance.Interval, Tile1Settings.Instance.IncludeSubfolders, Tile1Settings.Instance.Shuffle, clearExisting);
                        Tile1Settings.Instance.InitialUpdatesMade = true;
                        break;
                    case Constants.TILE_2_NUMBER:
                        Debug.WriteLine("Creating secondary tile 1 updates");
                        await CreateTileUpdates(Constants.Tile2Id, Tile2Settings.Instance.RootFolder, Constants.Tile2SaveFolder, Tile2Settings.Instance.Interval, Tile2Settings.Instance.IncludeSubfolders, Tile2Settings.Instance.Shuffle, clearExisting);
                        Tile2Settings.Instance.InitialUpdatesMade = true;
                        break;
                    case Constants.TILE_3_NUMBER:
                        Debug.WriteLine("Creating secondary tile 2 updates");
                        await CreateTileUpdates(Constants.Tile3Id, Tile3Settings.Instance.RootFolder, Constants.Tile3SaveFolder, Tile3Settings.Instance.Interval, Tile3Settings.Instance.IncludeSubfolders, Tile3Settings.Instance.Shuffle, clearExisting);
                        Tile3Settings.Instance.InitialUpdatesMade = true;
                        break;
                    case Constants.TILE_4_NUMBER:
                        Debug.WriteLine("Creating secondary tile 3 updates");
                        await CreateTileUpdates(Constants.Tile4Id, Tile4Settings.Instance.RootFolder, Constants.Tile4SaveFolder, Tile4Settings.Instance.Interval, Tile4Settings.Instance.IncludeSubfolders, Tile4Settings.Instance.Shuffle, clearExisting);
                        Tile4Settings.Instance.InitialUpdatesMade = true;
                        break;
                    default:
                        throw new NotImplementedException();

                }
            }).AsAsyncAction();
        }

        /// <summary>
        /// Calculates the dimensions to use to proportionally resize an image with the given dimensions to the given target dimensions.
        /// This function will return the original dimensions if the image is too small to be resized without stretching.
        /// </summary>
        /// <param name="originalWidth">The original width of the image to be resized.</param>
        /// <param name="originalHeight">The original height of the image to be resized.</param>
        /// <param name="targetWidth">The maximum width of the resized image.</param>
        /// <param name="targetHeight">The maximum height of the resized image.</param>
        /// <returns>The appropriate dimensions for the resized image within the target dimensions.</returns>
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