using Kozlowski.Slideshow.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Kozlowski.Slideshow
{
    public class Settings : INotifyPropertyChanged
    {
        private static Settings instance;
        private ApplicationDataContainer settings;
        private StorageFolder folder;

        private Settings()
        {
             settings = ApplicationData.Current.RoamingSettings;

             if (StorageApplicationPermissions.FutureAccessList.ContainsItem("ImagesLocation"))
                 folder = StorageApplicationPermissions.FutureAccessList.GetFolderAsync("ImagesLocation").GetResults();
             else
                 folder = KnownFolders.PicturesLibrary;
        }

        public static Settings Instance()
        {
            if (instance == null)
                instance = new Settings();

            return instance;
        }

        // Should be singleton?

        public int Index
        {
            get
            {
                if (settings.Values[Constants.SettingsName] == null)
                    settings.Values[Constants.SettingsName] = Constants.DefaultIntervalIndex;

                return (int)settings.Values[Constants.SettingsName];
            }
            set
            {
                settings.Values[Constants.SettingsName] = value;
                // NotifyPropertyChanged("Index");
                NotifyPropertyChanged("Interval");
            }
        }

        public int Interval
        {
            get
            {
                return Constants.IndexList[this.Index];
            }
        }

        public StorageFolder RootFolder
        {
            get
            {
                return folder;
            }

            set
            {
                if (value != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("ImagesLocation", value);
                    folder = value;
                    // NotifyPropertyChanged("RootFolder");
                    NotifyPropertyChanged("FolderPath");
                }
            }
        }

        public string FolderPath
        {
            get { return RootFolder.Path; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            Debug.WriteLine("NotifyPropertyChanged " + propertyName);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool IncludeSubfolders
        {
            get
            {
                if (settings.Values["Subfolders"] == null)
                    settings.Values["Subfolders"] = true;

                return (bool)settings.Values["Subfolders"];
            }
            set
            {
                settings.Values["Subfolders"] = value;
                NotifyPropertyChanged("IncludeSubfolders");
            }
        }
    }
}
