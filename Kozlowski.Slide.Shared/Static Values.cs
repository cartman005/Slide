using Windows.Foundation;
using Windows.Storage;
namespace Kozlowski.Slide.Shared
{
    /// <summary>
    /// This class represents all of the static values used in the app.
    /// </summary>
    public static class Constants
    {
        #region Background Tasks
        /// <summary>
        /// Gets the name of the timer background task.
        /// </summary>
        public static string TimerTaskName { get { return "SlideTimer"; } }

        /// <summary>
        /// Gets the name of the user present background task.
        /// </summary>
        public static string UserTaskName { get { return "SlideUserPresent"; } }

        /// <summary>
        /// Gets the entry point of the background tasks.
        /// </summary>
        public static string TaskEntryPoint { get { return "Kozlowski.Slide.Background.BackgroundTask"; } }
        #endregion

        #region Tile Numbers
        /// <summary>
        /// The number main tile (tile 1).
        /// This integer is needed for switch statements.
        /// </summary>
        public const int TILE_1_NUMBER = 1;

        /// <summary>
        /// The number of the first secondary tile (tile 2).
        /// This integer is needed for switch statements.
        /// </summary>
        public const int TILE_2_NUMBER = 2;

        /// <summary>
        /// The number of the second secondary tile (Tile 3).
        /// This integer is needed for switch statements.
        /// </summary>
        public const int TILE_3_NUMBER = 3;

        /// <summary>
        /// The ID of the third secondary tile (tile 4).
        /// This integer is needed for switch statements.
        /// </summary>
        public const int TILE_4_NUMBER = 4;

        /// <summary>
        /// Gets the number of the main tile.
        /// </summary>
        public static int Tile1Number { get { return TILE_1_NUMBER; } }

        /// <summary>
        /// Gets the number of the first secondary tile.
        /// </summary>
        public static int Tile2Number { get { return TILE_2_NUMBER; } }

        /// <summary>
        /// Gets the number of the second secondary tile.
        /// </summary>
        public static int Tile3Number { get { return TILE_3_NUMBER; } }

        /// <summary>
        /// Gets the number of the third secondary tile.
        /// </summary>
        public static int Tile4Number { get { return TILE_4_NUMBER; } }

        /// <summary>
        /// The ID of the main tile (tile 1).
        /// This is blank because a tile ID is not needed for the main app tile.
        /// This string is needed for switch statements.
        /// </summary>
        public const string TILE_1_ID = "";

        /// <summary>
        /// The ID of the first secondary tile (tile 2).
        /// This string is needed for switch statements.
        /// </summary>
        public const string TILE_2_ID = "SlideTile2";

        /// <summary>
        /// The ID of the second secondary tile (Tile 3).
        /// This string is needed for switch statements.
        /// </summary>
        public const string TILE_3_ID = "SlideTile3";

        /// <summary>
        /// The ID of the third secondary tile (tile 4).
        /// This string is needed for switch statements.
        /// </summary>
        public const string TILE_4_ID = "SlideTile4";

        /// <summary>
        /// Gets the ID of the main tile (tile 1).
        /// </summary>
        public static string Tile1Id { get { return TILE_1_ID; } }

        /// <summary>
        /// Gets the ID of the first secondary tile (tile 2).
        /// </summary>
        public static string Tile2Id { get { return TILE_2_ID; } }

        /// <summary>
        /// Gets the ID of the third secondary tile (tile 3).
        /// </summary>
        public static string Tile3Id { get { return TILE_3_ID; } }

        /// <summary>
        /// Gets the ID of the third secondary tile (tile 4).
        /// </summary>
        public static string Tile4Id { get { return TILE_4_ID; } }
        #endregion

        #region Settings Names
        // Persistent
        /// <summary>
        /// Gets the name of the interval setting.
        /// </summary>
        public static string SettingsName_Interval { get { return "Interval"; } }

        /// <summary>
        /// Gets the name of the image location setting.
        /// </summary>
        public static string SettingsName_ImagesLocation { get { return "FolderPath"; } }

        /// <summary>
        /// Gets the name of the include subfolder settinge.
        /// </summary>
        public static string SettingsName_Subfolders { get { return "IncludeSubfolders"; } }

        /// <summary>
        /// Gets the name of the shuffle setting.
        /// </summary>
        public static string SettingsName_Shuffle { get { return "Shuffle"; } }

        /// <summary>
        /// Gets the name of the animation setting.
        /// </summary>
        public static string SettingsName_Animate { get { return "Animate"; } }

        /// <summary>
        /// Gets the name of the name of the switch that indicates that the first set of tile updates have been created.
        /// This should be done on the first run of the tile.
        /// </summary>
        public static string SettingsName_InitialUpdatesMade { get { return "InitialUpdatesMade"; } }

        /// <summary>
        /// Gets the name of the name of the setting that stores the first file to begin with when retrieving ordered images for tile updates.
        /// </summary>
        public static string SettingsName_StartingFilename { get { return "StartingFilename"; } }

        // Temporary
        /// <summary>
        /// Gets the name of the selected index setting for the FlipView.
        /// </summary>
        public static string SettingsName_SelectedIndex { get { return "SelectedIndex"; } }

        /// <summary>
        /// Gets the name of the paused setting.
        /// </summary>
        public static string SettingsName_IsPaused { get { return "IsPaused"; } }
        #endregion

        #region File Names
        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for the main tile (tile 1).
        /// </summary>
        public static string Tile1SaveFolder { get { return "Tile1"; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for the first secondary tile (tile 2).
        /// </summary>
        public static string Tile2SaveFolder { get { return "Tile2"; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for the second secondary tile (tile 3).
        /// </summary>
        public static string Tile3SaveFolder { get { return "Tile3"; } }

        /// <summary>
        /// Gets the name of the folder in which to store the cached tile update images in the AppData folder for the third secondary tile (tile 4).
        /// </summary>
        public static string Tile4SaveFolder { get { return "Tile4"; } }

        /// <summary>
        /// Gets the name of the file for storing the serialized Items collection in the AppData folder.
        /// </summary>
        public static string CollectionFileName { get { return "SlideItemsCollection.xml"; } }
        #endregion

        #region Commands
        /// <summary>
        /// The try again message.
        /// </summary>
        public const string TRY_AGAIN = "Try again";

        /// <summary>
        /// The close message.
        /// </summary>
        public const string CLOSE = "Close";
        #endregion

        #region Image Counts
        /// <summary>
        /// Gets the number of images to load incrementally each time LoadMoreFiles is called.
        /// </summary>
        public static int ImagesToLoad { get { return 12; } }

        /// <summary>
        /// Gets the minimum number of future images to have loaded at all times for the full screen slide show.
        /// </summary>
        public static int ImageLoadBuffer { get { return 3; } }

        /// <summary>
        /// Gets the number of the maximum number of image files to keep in the file list.
        /// </summary>
        public static int MaxCount { get { return 300; } }

        /// <summary>
        /// Gets the array of intervals, in seconds, associated with each index of the combo box.
        /// </summary>
        public static int[] IndexList { get { return new int[] { 5, 10, 30, 60, 180 }; } }
        #endregion

        #region Defaults
        /// <summary>
        /// Returns the default folder to find images for the slide show.
        /// </summary>
        public static StorageFolder DefaultFolder { get { return KnownFolders.PicturesLibrary; } }

        /// <summary>
        /// Gets the combo box index of the default interval setting.
        /// </summary>
        public static int DefaultIntervalIndex { get { return 2; } }

        /// <summary>
        /// Gets the default setting for including subfolders in the image location.
        /// </summary>
        public static bool DefaultSubfoldersSetting { get { return true; } }

        /// <summary>
        /// Gets the default setting for shuffling the image display order.
        /// </summary>
        public static bool DefaultShuffleSetting { get { return true; } }

        /// <summary>
        /// Gets the default setting for animating the images.
        /// </summary>
        public static bool DefaultAnimateSetting { get { return true; } }

        /// <summary>
        /// Gets the interval for scheduling the timer background task, in minutes.
        /// This cannot be less than 15 minutes.
        /// </summary>
        public static int BackgroundTaskInterval { get { return 30; } }

        /// <summary>
        /// Gets the decimal by which to scale the images when animating.
        /// </summary>
        public static double ScaleDecimal { get { return 1.25; } }

        /// <summary>
        /// Gets the number of tile updates that are allowed to overlap.
        /// A higher number ensures that no gaps exist in the tile updates.
        /// No more than 6 updates can exist at the same time.
        /// </summary>
        public static double ConcurrentTiles { get { return 3; } }

        /// <summary>
        /// Gets the dimensions of the maximum tile size available for the app.
        /// This is currently 310x310.
        /// </summary>
        public static Size MaxTileSize { get { return new Size(310, 310); } }
        #endregion
    }
}