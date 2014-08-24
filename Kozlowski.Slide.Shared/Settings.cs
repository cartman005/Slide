using Kozlowski.Slide.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Kozlowski.Slide
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
                 Task.Run(
                    async() =>
                    {
                        folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("ImagesLocation");
                    }).Wait();
             else
                 folder = KnownFolders.PicturesLibrary;
        }

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                    instance = new Settings();

                return instance;
            }
        }

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
                NotifyPropertyChanged("Interval");
            }
        }

        public int Interval
        {
            get { return Constants.IndexList[this.Index]; }
        }

        public StorageFolder RootFolder
        {
            get { return folder; }

            set
            {
                if (value != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace("ImagesLocation", value);
                    folder = value;
                    NotifyPropertyChanged("FolderPath");
                }
            }
        }

        public string FolderPath
        {
            get
            { 
                var path = RootFolder.Path;
                Debug.WriteLine("The path is " + path);
                if (path == "")
                    path = RootFolder.DisplayName;
                return path;
            }
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

        public bool Shuffle
        {
            get
            {
                if (settings.Values["Shuffle"] == null)
                    settings.Values["Shuffle"] = true;

                return (bool)settings.Values["Shuffle"];
            }
            set
            {
                settings.Values["Shuffle"] = value;
                NotifyPropertyChanged("Shuffle");
            }
        }

        public bool ImagesFound
        {
            get
            {
                if (settings.Values["ImagesFound"] == null)
                    settings.Values["ImagesFound"] = true;

                return (bool)settings.Values["ImagesFound"];
            }
            set
            {
                settings.Values["ImagesFound"] = value;
                NotifyPropertyChanged("ImagesFound");
            }
        }
    }
}
