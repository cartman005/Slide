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
    public sealed class BackgroundTileMaker : IBackgroundTask
    {
        /// <summary>
        /// Performs the work of the background task.
        /// </summary>
        /// <param name="taskInstance">The instance of the background process.</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {        
            var defferal = taskInstance.GetDeferral();
            Debug.WriteLine("Started the background task");
            Settings settings;

            // Main tile
            Debug.WriteLine("Creating primary tile updates");
            settings = MainSettings.Instance;
            await TileMaker.GenerateTiles(Constants.MainTileUpdatesFolder, "", settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);

            // Secondary tile 1
            if (SecondaryTile.Exists(Constants.Secondary1TileId))
            {
                Debug.WriteLine("Creating secondary 1 tile updates");
                settings = Secondary1Settings.Instance;
                await TileMaker.GenerateTiles(Constants.Secondary1TileUpdatesFolder, Constants.Secondary1TileId, settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            // Secondary tile 2
            if (SecondaryTile.Exists(Constants.Secondary2TileId))
            {
                Debug.WriteLine("Creating secondary 2 tile updates");
                settings = Secondary2Settings.Instance;
                await TileMaker.GenerateTiles(Constants.Secondary2TileUpdatesFolder, Constants.Secondary2TileId, settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            // Secondary tile 3
            if (SecondaryTile.Exists(Constants.Secondary3TileId))
            {
                Debug.WriteLine("Creating secondary 3 tile updates");
                settings = Secondary3Settings.Instance;
                await TileMaker.GenerateTiles(Constants.Secondary3TileUpdatesFolder, Constants.Secondary3TileId, settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
