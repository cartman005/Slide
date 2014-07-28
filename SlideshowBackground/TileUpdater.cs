using Slideshow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace SlideshowBackground
{
    public sealed class TileUpdater : IBackgroundTask
    {
        private ApplicationDataContainer settings = null;
        private IReadOnlyList<StorageFile> fileList;

        public TileUpdater()
        {
            Debug.WriteLine("----------------------");
            settings = ApplicationData.Current.RoamingSettings;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {            
            var defferal = taskInstance.GetDeferral();
            Debug.WriteLine("Background task running-----------");
            int index;
            settings = ApplicationData.Current.RoamingSettings;
            if (settings.Values.ContainsKey(MainPage.SETTINGS_NAME))
            {
                index = (int)(settings.Values[MainPage.SETTINGS_NAME]);
            }
            else
            {
                index = MainPage.DEFAULT_INTERVAL_INDEX;
            }

            Debug.WriteLine("Background Task GO " + index);

            fileList = await GetImages.GetImageList(KnownFolders.PicturesLibrary);

            GetImages.Run(MainPage.INDEX_LIST[index], fileList);

            defferal.Complete();
        }

    }
}
