using Kozlowski.Slideshow.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Notifications;

namespace Kozlowski.Slideshow.Background
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
            await TileMaker.CreateTiles(settings.Interval, fileList);
            Debug.WriteLine("Finished the background task");
            // settings.PropertyChanged += settings_PropertyChanged;
            defferal.Complete();
        }

        // TODO Can this work in Windows Runtime?
        /* 
        private async void settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Running property changed from background");

            if (e.PropertyName == "FolderPath")
            {
                fileList.Clear();
                fileList.AddRange(await TileMaker.GetImageList(settings.RootFolder, settings.IncludeSubfolders));
            }
            await TileMaker.CreateTiles(settings.Interval, fileList);
        }
         */
    }
}
