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
    public abstract class Settings : INotifyPropertyChanged
    {
        private ApplicationDataContainer settings;
        private StorageFolder rootFolder;
        private string Id;
               
        /// <summary>
        /// Private constructor that opens the app's Roaming Settings and the FutureAccessList to find the current root folder.
        /// </summary>
        public Settings(string tileId)
        {
            Debug.WriteLine("Settings constructor called.");
            Id = tileId;
            settings = ApplicationData.Current.RoamingSettings;

            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(Constants.SettingsName_ImagesLocation + Id))
                Task.Run(
                   async () =>
                   {
                       rootFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(Constants.SettingsName_ImagesLocation + Id);
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
                if (settings.Values[Constants.SettingsName_InitialUpdatesMade + Id] == null)
                    settings.Values[Constants.SettingsName_InitialUpdatesMade + Id] = false;

                return (bool)settings.Values[Constants.SettingsName_InitialUpdatesMade + Id];
            }
            set
            {
                settings.Values[Constants.SettingsName_InitialUpdatesMade + Id] = value;
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected interval in the settings flyout combo box.
        /// </summary>
        public int Index
        {
            get
            {
                if (settings.Values[Constants.SettingsName_Interval + Id] == null)
                    settings.Values[Constants.SettingsName_Interval + Id] = Constants.DefaultIntervalIndex;

                return (int)settings.Values[Constants.SettingsName_Interval + Id];
            }
            set
            {
                settings.Values[Constants.SettingsName_Interval + Id] = value;
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
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(Constants.SettingsName_ImagesLocation + Id, value);
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
                if (settings.Values[Constants.SettingsName_Subfolders + Id] == null)
                    settings.Values[Constants.SettingsName_Subfolders + Id] = Constants.DefaultSubfoldersSetting;

                return (bool)settings.Values[Constants.SettingsName_Subfolders + Id];
            }
            set
            {
                settings.Values[Constants.SettingsName_Subfolders + Id] = value;
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
                if (settings.Values[Constants.SettingsName_Shuffle + Id] == null)
                    settings.Values[Constants.SettingsName_Shuffle + Id] = Constants.DefaultShuffleSetting;

                return (bool)settings.Values[Constants.SettingsName_Shuffle + Id];
            }
            set
            {
                settings.Values[Constants.SettingsName_Shuffle + Id] = value;
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
                if (settings.Values[Constants.SettingsName_Animate + Id] == null)
                    settings.Values[Constants.SettingsName_Animate + Id] = Constants.DefaultAnimateSetting;

                return (bool)settings.Values[Constants.SettingsName_Animate + Id];
            }
            set
            {
                settings.Values[Constants.SettingsName_Animate + Id] = value;
                NotifyPropertyChanged(Constants.SettingsName_Animate);
            }
        }

        public string TileId { get { return Id; } }

        public abstract string SaveFolder { get; }
        
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

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings.
    /// </summary>
    public class MainSettings : Settings
    {
        // The single instance of the class
        private static readonly MainSettings instance = new MainSettings("");

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings classed.
        /// </summary>
        public static MainSettings Instance { get { return instance; } }

        private MainSettings(string Id)
            : base(Id)
        {
        }

        public override string SaveFolder { get { return Constants.MainTileUpdatesFolder; } }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings.
    /// </summary>
    public class Secondary1Settings : Settings
    {
        // The single instance of the class
        private static readonly Secondary1Settings instance = new Secondary1Settings(Constants.Secondary1TileId);

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings classed.
        /// </summary>
        public static Secondary1Settings Instance { get { return instance; } }

        private Secondary1Settings(string Id)
            : base(Id)
        {
        }

        public override string SaveFolder { get { return Constants.Secondary1TileUpdatesFolder; } }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings.
    /// </summary>
    public class Secondary2Settings : Settings
    {
        // The single instance of the class
        private static readonly Secondary2Settings instance = new Secondary2Settings(Constants.Secondary2TileId);

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings classed.
        /// </summary>
        public static Secondary2Settings Instance { get { return instance; } }

        private Secondary2Settings(string Id)
            : base(Id)
        {
        }

        public override string SaveFolder { get { return Constants.Secondary2TileUpdatesFolder; } }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings.
    /// </summary>
    public class Secondary3Settings : Settings
    {
        // The single instance of the class
        private static readonly Secondary3Settings instance = new Secondary3Settings(Constants.Secondary3TileId);

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings classed.
        /// </summary>
        public static Secondary3Settings Instance { get { return instance; } }

        private Secondary3Settings(string Id)
            : base(Id)
        {
        }

        public override string SaveFolder { get { return Constants.Secondary3TileUpdatesFolder; } }
    }
}
