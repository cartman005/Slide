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
        private ApplicationDataContainer settings = null;

        public TileUpdater()
        {
            settings = ApplicationData.Current.RoamingSettings;
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {            
            var defferal = taskInstance.GetDeferral();
            int index;
            settings = ApplicationData.Current.RoamingSettings;
            if (settings.Values.ContainsKey(Constants.SettingsName))
            {
                index = (int)(settings.Values[Constants.SettingsName]);
            }
            else
            {
                index = Constants.DefaultIntervalIndex;
            }
                        
            await TileMaker.CreateTiles(Constants.IndexList[index]);
            Debug.WriteLine("Finished the background task");
            defferal.Complete();
        }
    }
}
