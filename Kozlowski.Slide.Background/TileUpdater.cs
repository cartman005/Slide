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
            var settings = Settings.Instance;
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle));
            Debug.WriteLine("Creating primary tile updates");
            await TileMaker.GenerateTiles("", settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);

            if (SecondaryTile.Exists(Constants.SecondaryTileId1))
            {
                Debug.WriteLine("Creating secondary tile updates");
                await TileMaker.GenerateTiles(Constants.SecondaryTileId1, settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            }

            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
