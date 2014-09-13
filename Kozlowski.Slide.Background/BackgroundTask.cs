using Kozlowski.Slide.Shared;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.UI.StartScreen;

namespace Kozlowski.Slide.Background
{
    /// <summary>
    /// This class is used to run the background task which creates and schedules the Start screen tile updates.
    /// </summary>
    public sealed class BackgroundTask : IBackgroundTask
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

            // Main tile
            await TileMaker.CreateTileUpdates(Constants.Tile1Number, false);

            // Tile 2
            if (SecondaryTile.Exists(Constants.Tile2Id))
                await TileMaker.CreateTileUpdates(Constants.Tile2Number, false);

            // Tile 3
            if (SecondaryTile.Exists(Constants.Tile3Id))
                await TileMaker.CreateTileUpdates(Constants.Tile3Number, false);

            // Tile 4
            if (SecondaryTile.Exists(Constants.Tile4Id))
                await TileMaker.CreateTileUpdates(Constants.Tile4Number, false);

            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
