using Kozlowski.Slide.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Kozlowski.Slide
{
    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings.
    /// </summary>
    public class Settings : INotifyPropertyChanged
    {
        // The single instance of the class
        private static readonly Settings instance = new Settings();
        private ApplicationDataContainer settings;
        private StorageFolder rootFolder;
        
        /// <summary>
        /// Gets the app's thread-safe instance of the Settings classed.
        /// </summary>
        public static Settings Instance { get { return instance; } }
        
        /// <summary>
        /// Private constructor that opens the app's Roaming Settings and the FutureAccessList to find the current root folder.
        /// </summary>
        private Settings()
        {
            Debug.WriteLine("Settings constructor called.");
            settings = ApplicationData.Current.RoamingSettings;

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(Constants.SettingsName_ImagesLocation))
                Task.Run(
                   async () =>
                   {
                       rootFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Constants.SettingsName_ImagesLocation);
                   }).Wait();
            else
                rootFolder = KnownFolders.PicturesLibrary;
        }

        /// <summary>
        /// Gets or sets whether or not the first set of tile updates have been created by the app.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the index of the selected interval in the settings flyout combo box.
        /// </summary>
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
                NotifyPropertyChanged(Constants.SettingsName_Interval);
            }
        }

        /// <summary>
        /// Gets the time interval selected in the settings flyout in seconds.
        /// </summary>
        public int Interval
        {
            get { return Constants.IndexList[this.Index]; }
        }

        /// <summary>
        /// Gets or sets the root folder for the image files.
        /// </summary>
        public StorageFolder RootFolder
        {
            get { return rootFolder; }

            set
            {
                if (value != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(Constants.SettingsName_ImagesLocation, value);
                    rootFolder = value;
                    NotifyPropertyChanged(Constants.SettingsName_ImagesLocation);
                }
            }
        }

        /// <summary>
        /// Gets the path of the folder selected as the root folder; or if the path is blank due to the folder being a virtual folder, gets the display name of the folder.
        /// </summary>
        public string FolderPath
        {
            get
            { 
                string path = RootFolder.Path;

                // Paths for virtual folders like Libraries are blank
                if (path == "")
                    path = RootFolder.DisplayName;
                return path;
            }
        }

        /// <summary>
        /// Gets or sets whether subfolders of the selected image folder should be included in the slideshow.
        /// </summary>
        public bool IncludeSubfolders
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Subfolders] == null)
                    settings.Values[Constants.SettingsName_Subfolders] = Constants.DefaultSubfoldersSetting;

                return (bool)settings.Values[Constants.SettingsName_Subfolders];
            }
            set
            {
                settings.Values[Constants.SettingsName_Subfolders] = value;
                NotifyPropertyChanged(Constants.SettingsName_Subfolders);
            }
        }

        /// <summary>
        /// Gets or sets whether the order that images are displayed should be randomized (shuffled).
        /// </summary>
        public bool Shuffle
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Shuffle] == null)
                    settings.Values[Constants.SettingsName_Shuffle] = Constants.DefaultShuffleSetting;

                return (bool)settings.Values[Constants.SettingsName_Shuffle];
            }
            set
            {
                settings.Values[Constants.SettingsName_Shuffle] = value;
                NotifyPropertyChanged(Constants.SettingsName_Shuffle);
            }
        }
        
        /// <summary>
        /// Gets or sets whether the images displayed should be animated.
        /// </summary>
        public bool Animate
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Animate] == null)
                    settings.Values[Constants.SettingsName_Animate] = Constants.DefaultAnimateSetting;

                return (bool)settings.Values[Constants.SettingsName_Animate];
            }
            set
            {
                settings.Values[Constants.SettingsName_Animate] = value;
                NotifyPropertyChanged(Constants.SettingsName_Animate);
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
