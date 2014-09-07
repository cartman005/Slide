using Kozlowski.Slide.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Kozlowski.Slide.Background
{
    /// <summary>
    /// This class is used to run the background task which creates and schedules the Start screen tile updates.
    /// </summary>
    public sealed class TileUpdateMaker : IBackgroundTask
    {
        /// <summary>
        /// Runs the background task.
        /// </summary>
        /// <param name="taskInstance">The instance of the background process.</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {        
            var defferal = taskInstance.GetDeferral();
            Debug.WriteLine("Started the background task");
            var fileList = new List<StorageFile>();
            Settings settings;

            // Main tile
            Debug.WriteLine("Creating primary tile updates");
            settings = MainSettings.Instance;
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle));
            await TileMaker.GenerateTiles(Constants.MainTileUpdatesFolder, "", settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);

            // Secondary tile 1
            if (SecondaryTile.Exists(Constants.Secondary1TileId))
            {
                Debug.WriteLine("Creating secondary 1 tile updates");
                fileList.Clear();
                settings = Secondary1Settings.Instance;
                fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle));
                await TileMaker.GenerateTiles(Constants.Secondary1TileUpdatesFolder, Constants.Secondary1TileId, settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            // Secondary tile 2
            if (SecondaryTile.Exists(Constants.Secondary2TileId))
            {
                Debug.WriteLine("Creating secondary 2 tile updates");
                fileList = new List<StorageFile>();
                settings = Secondary2Settings.Instance;
                fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle));
                await TileMaker.GenerateTiles(Constants.Secondary2TileUpdatesFolder, Constants.Secondary2TileId, settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            // Secondary tile 3
            if (SecondaryTile.Exists(Constants.Secondary3TileId))
            {
                Debug.WriteLine("Creating secondary 3 tile updates");
                fileList = new List<StorageFile>();
                settings = Secondary3Settings.Instance;
                fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle));
                await TileMaker.GenerateTiles(Constants.Secondary3TileUpdatesFolder, Constants.Secondary3TileId, settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
