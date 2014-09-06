using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Kozlowski.Slide.Background
{
    /// <summary>
    /// This class is used to run the background task which creates and schedules the Start screen tile updates.
    /// </summary>
    public sealed class TileUpdater : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {            
            var defferal = taskInstance.GetDeferral();
            Debug.WriteLine("Startd the background task");
            var fileList = new List<StorageFile>();
            var settings = Settings.Instance;
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle));            
            await TileMaker.GenerateTiles(settings.Interval, fileList, settings.RootFolder, settings.IncludeSubfolders, settings.Shuffle);
            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
