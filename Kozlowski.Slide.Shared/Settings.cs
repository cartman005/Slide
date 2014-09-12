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

        /// <summary>
        /// Private constructor that opens the app's Roaming Settings and the FutureAccessList to find the current root folder.
        /// </summary>
        public Settings()
        {
            Debug.WriteLine("Settings constructor called.");
            settings = ApplicationData.Current.LocalSettings;

            // Get root folder now since requires an asynchronous function
            if (StorageApplicationPermissions.FutureAccessList.ContainsItem(GetTileSpecificSettingName(Constants.SettingsName_ImagesLocation)))
                Task.Run(
                   async () =>
                   {
                       rootFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(GetTileSpecificSettingName(Constants.SettingsName_ImagesLocation));
                   }).Wait();
            else
                rootFolder = Constants.DefaultFolder;
        }

        /// <summary>
        /// Gets or sets whether or not the first set of tile updates have been created by the app.
        /// </summary>
        public bool InitialUpdatesMade
        {
            get
            {
                return GetSetting<bool>(Constants.SettingsName_InitialUpdatesMade, false);
            }
            set
            {
                SetSetting<bool>(Constants.SettingsName_InitialUpdatesMade, value);
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected interval in the settings flyout combo box.
        /// </summary>
        public int Index
        {
            get
            {
                return GetSetting<int>(Constants.SettingsName_Interval, Constants.DefaultIntervalIndex);
            }
            set
            {
                SetSetting<int>(Constants.SettingsName_Interval, value);
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
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(GetTileSpecificSettingName(Constants.SettingsName_ImagesLocation), value);
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
                return GetSetting<bool>(Constants.SettingsName_Subfolders, Constants.DefaultSubfoldersSetting);
            }
            set
            {
                SetSetting<bool>(Constants.SettingsName_Subfolders, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the order that images are displayed should be randomized (shuffled).
        /// </summary>
        public bool Shuffle
        {
            get
            {
                return GetSetting<bool>(Constants.SettingsName_Shuffle, Constants.DefaultShuffleSetting);
            }
            set
            {
                SetSetting<bool>(Constants.SettingsName_Shuffle, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the images displayed should be animated.
        /// </summary>
        public bool Animate
        {
            get
            {
                return GetSetting<bool>(Constants.SettingsName_Animate, Constants.DefaultAnimateSetting);
            }
            set
            {
                SetSetting<bool>(Constants.SettingsName_Animate, value);
            }
        }
        
        /// <summary>
        /// Gets the ID of the tile for creating secondary tiles.
        /// </summary>
        public abstract string TileId { get; }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for the specific tile represented by the Settings instance.
        /// </summary>
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

        /// <summary>
        /// Helper function to get a setting in the LocalSettings file for the tile represented by this particular Settings instance.
        /// Creates the value if it does not already exists.
        /// </summary>
        /// <typeparam name="T">The type of the value to be returned.</typeparam>
        /// <param name="baseSettingName">The base name of the setting which is the same for all tiles.</param>
        /// <param name="defaultValue">The value to be returned if no previously-stored value is found.</param>
        private T GetSetting<T>(string baseSettingName, T defaultValue)
        {
            if (settings.Values[GetTileSpecificSettingName(baseSettingName)] == null)
                settings.Values[GetTileSpecificSettingName(baseSettingName)] = defaultValue;

            return (T)settings.Values[GetTileSpecificSettingName(baseSettingName)];
        }

        /// <summary>
        /// Helper function to update a setting in the LocalSettings file for the tile represented by this particular Settings instance.
        /// Sends a notification that the setting has been updated.
        /// </summary>
        /// <typeparam name="T">The type of the value to be stored.</typeparam>
        /// <param name="baseSettingName">The base name of the setting which is the same for all tiles.</param>
        /// <param name="value">The value to be stored in the settings file.</param>
        private void SetSetting<T>(string baseSettingName, T value)
        {
            settings.Values[GetTileSpecificSettingName(baseSettingName)] = value;
            NotifyPropertyChanged(baseSettingName);
        }

        /// <summary>
        /// Helper function to find the name of a setting in the LocalSettings file for the tile represented by this particular Settings instance.
        /// </summary>
        /// <param name="baseSettingName">The base name of the setting which is the same for all tiles.</param>
        /// <returns>The full name of the setting, specific to a single tile.</returns>
        private string GetTileSpecificSettingName(string baseSettingName)
        {
            return string.Format("{0}_{1}", baseSettingName, TileId);
        }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings for the main/default tile (tile 1).
    /// </summary>
    public class Tile1Settings : Settings
    {
        // The single instance of the class
        private static readonly Tile1Settings instance = new Tile1Settings();

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings class for tile 1.
        /// </summary>
        public static Tile1Settings Instance { get { return instance; } }

        /// <summary>
        /// Calls the base constructor.
        /// </summary>
        private Tile1Settings(): base() { }

        /// <summary>
        /// Gets the ID of the tile 1.
        /// This is blank because a tile ID is not needed for the main app tile. 
        /// </summary>
        public override string TileId { get { return Constants.Tile1Id; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for tile 1.
        /// </summary>
        public override string SaveFolder { get { return Constants.Tile1SaveFolder; } }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings for tile 2.
    /// </summary>
    public class Tile2Settings : Settings
    {
        // The single instance of the class
        private static readonly Tile2Settings instance = new Tile2Settings();

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings class for tile 2.
        /// </summary>
        public static Tile2Settings Instance { get { return instance; } }

        /// <summary>
        /// Calls the base constructor.
        /// </summary>
        private Tile2Settings(): base() { }

        /// <summary>
        /// Gets the ID of tile 2.
        /// </summary>
        public override string TileId { get { return Constants.Tile2Id; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for tile 2.
        /// </summary>
        public override string SaveFolder { get { return Constants.Tile2SaveFolder; } }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings for tile 3.
    /// </summary>
    public class Tile3Settings : Settings
    {
        // The single instance of the class
        private static readonly Tile3Settings instance = new Tile3Settings();

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings class for tile 3.
        /// </summary>
        public static Tile3Settings Instance { get { return instance; } }

        /// <summary>
        /// Calls the base constructor.
        /// </summary>
        private Tile3Settings(): base() { }

        /// <summary>
        /// Gets the ID of tile 3.
        /// </summary>
        public override string TileId { get { return Constants.Tile3Id; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for tile 3.
        /// </summary>
        public override string SaveFolder { get { return Constants.Tile3SaveFolder; } }
    }

    /// <summary>
    /// This class is used to provide a singleton, central access point to the app's settings for tile 4.
    /// </summary>
    public class Tile4Settings : Settings
    {
        // The single instance of the class
        private static readonly Tile4Settings instance = new Tile4Settings();

        /// <summary>
        /// Gets the app's thread-safe instance of the Settings class for tile 4.
        /// </summary>
        public static Tile4Settings Instance { get { return instance; } }

        /// <summary>
        /// Calls the base constructor.
        /// </summary>
        private Tile4Settings(): base() { }

        /// <summary>
        /// Gets the ID of tile 4.
        /// </summary>
        public override string TileId { get { return Constants.Tile4Id; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for tile 4.
        /// </summary>
        public override string SaveFolder { get { return Constants.Tile4SaveFolder; } }
    }
}
