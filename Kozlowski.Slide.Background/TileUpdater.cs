using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace Kozlowski.Slide.Background
{
    public sealed class TileUpdater : IBackgroundTask
    {
        private Settings settings;
        private List<StorageFile> fileList;

        public TileUpdater()
        {
            settings = Settings.Instance;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {            
            var defferal = taskInstance.GetDeferral();
            fileList = new List<StorageFile>();
            fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));            
            await TileMaker.GenerateTiles(settings.Interval, fileList);
            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }        
    }
}
