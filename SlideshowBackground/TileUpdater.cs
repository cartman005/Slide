﻿using System;
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
            Debug.WriteLine("Background task running");
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

            Debug.WriteLine("Using index " + index);
            
            await TileMaker.CreateTiles(Constants.IndexList[index]);
            Debug.WriteLine("Background task done");
            defferal.Complete();
        }
    }
}
