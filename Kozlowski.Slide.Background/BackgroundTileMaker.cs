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
        /// Generates tile updates for each tile that is currently pinned to the Start screen.
        /// </summary>
        /// <param name="taskInstance">The instance of the background process.</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {        
            var defferal = taskInstance.GetDeferral();
            Debug.WriteLine("Started the background task");
            Settings settings;

            // Main tile
            Debug.WriteLine("Creating primary tile updates");
            settings = Tile1Settings.Instance;
            await TileMaker.GenerateTiles(Constants.Tile1SaveFolder, "", settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle, false);

            // Tile 2
            if (SecondaryTile.Exists(Constants.Tile2Id))
            {
                Debug.WriteLine("Creating secondary 1 tile updates");
                settings = Tile2Settings.Instance;
                await TileMaker.GenerateTiles(Constants.Tile2SaveFolder, Constants.Tile2Id, settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle, false);
            }

            // Tile 3
            if (SecondaryTile.Exists(Constants.Tile3Id))
            {
                Debug.WriteLine("Creating secondary 2 tile updates");
                settings = Tile3Settings.Instance;
                await TileMaker.GenerateTiles(Constants.Tile3SaveFolder, Constants.Tile3Id, settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle, false);
            }

            // Tile 4
            if (SecondaryTile.Exists(Constants.Tile4Id))
            {
                Debug.WriteLine("Creating secondary 3 tile updates");
                settings = Tile4Settings.Instance;
                await TileMaker.GenerateTiles(Constants.Tile4SaveFolder, Constants.Tile4Id, settings.Interval, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle, false);
            }

            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
