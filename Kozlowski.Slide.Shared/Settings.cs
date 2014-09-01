using Kozlowski.Slide.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
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

             if (StorageApplicationPermissions.FutureAccessList.ContainsItem(Constants.SettingsName_ImagesLocation))
                 Task.Run(
                    async() =>
                    {
                        folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Constants.SettingsName_ImagesLocation);
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

        public bool InitialUpdatesMade
        {
            get
            {
                if (settings.Values[Constants.SettingsName_InitialUpdatesMade] == null)
                    settings.Values[Constants.SettingsName_InitialUpdatesMade] = false;

                return (bool)settings.Values[Constants.SettingsName_InitialUpdatesMade];
            }
            set
            {
                settings.Values[Constants.SettingsName_InitialUpdatesMade] = value;
            }
        }

        public int Index
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Interval] == null)
                    settings.Values[Constants.SettingsName_Interval] = Constants.DefaultIntervalIndex;

                return (int)settings.Values[Constants.SettingsName_Interval];
            }
            set
            {
                settings.Values[Constants.SettingsName_Interval] = value;
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
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(Constants.SettingsName_ImagesLocation, value);
                    folder = value;
                    NotifyPropertyChanged("FolderPath");
                }
            }
        }

        public string FolderPath
        {
            get
            { 
                string path = RootFolder.Path;
                if (path == "")
                    path = RootFolder.DisplayName;
                return path;
            }
        }

        public bool IncludeSubfolders
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Subfolders] == null)
                    settings.Values[Constants.SettingsName_Subfolders] = true;

                return (bool)settings.Values[Constants.SettingsName_Subfolders];
            }
            set
            {
                settings.Values[Constants.SettingsName_Subfolders] = value;
                NotifyPropertyChanged("IncludeSubfolders");
            }
        }

        public bool Shuffle
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Shuffle] == null)
                    settings.Values[Constants.SettingsName_Shuffle] = true;

                return (bool)settings.Values[Constants.SettingsName_Shuffle];
            }
            set
            {
                settings.Values[Constants.SettingsName_Shuffle] = value;
                NotifyPropertyChanged("Shuffle");
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
    }
}
