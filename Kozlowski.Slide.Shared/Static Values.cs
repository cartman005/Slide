
namespace Kozlowski.Slide.Shared
{
    /// <summary>
    /// This class represents all of the static values used in the app.
    /// </summary>
    public static class Constants
    {
        // Background tasks
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
        public static string TaskEntryPoint { get { return "Kozlowski.Slide.Background.TileUpdater"; } }

        /// <summary>
        /// Gets the ID of the first secondary tile.
        /// </summary>
        public static string Secondary1TileId { get { return "SlideSecondaryTile1"; } }

        /// <summary>
        /// Gets the ID of the second secondary tile.
        /// </summary>
        public static string Secondary2TileId { get { return "SlideSecondaryTile2"; } }

        /// <summary>
        /// Gets the ID of the third secondary tile.
        /// </summary>
        public static string Secondary3TileId { get { return "SlideSecondaryTile3"; } }

        /// <summary>
        /// Gets the interval for scheduling the timer background task.
        /// </summary>
        public static int BackgroundTaskInterval { get { return 15; } }

        // Settings names
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
        /// Gets the name of the name of the switch that 
        /// </summary>
        public static string SettingsName_InitialUpdatesMade { get { return "InitialUpdatesMade"; } }

        // Temporary
        /// <summary>
        /// Gets the name of the selected index setting for the FlipView.
        /// </summary>
        public static string SettingsName_SelectedIndex { get { return "SelectedIndex"; } }

        /// <summary>
        /// Gets the name of the paused setting.
        /// </summary>
        public static string SettingsName_IsPaused { get { return "IsPaused"; } }

        // File names
        /// <summary>
        /// Gets the name of the folder to store the cached tile update images in the AppData folder.
        /// </summary>
        public static string TileUpdatesFolder { get { return "TileUpdates"; } }

        /// <summary>
        /// Gets the name of the folder to store the cached tile update images in the AppData folder.
        /// </summary>
        public static string MainTileUpdatesFolder { get { return "MainTile"; } }

        /// <summary>
        /// Gets the name of the folder to store the cached tile update images in the AppData folder.
        /// </summary>
        public static string Secondary1TileUpdatesFolder { get { return "SecondaryTile1"; } }

        /// <summary>
        /// Gets the name of the folder to store the cached tile update images in the AppData folder.
        /// </summary>
        public static string Secondary2TileUpdatesFolder { get { return "SecondaryTile2"; } }

        /// <summary>
        /// Gets the name of the folder to store the cached tile update images in the AppData folder.
        /// </summary>
        public static string Secondary3TileUpdatesFolder { get { return "SecondaryTile3"; } }

        /// <summary>
        /// Gets the name of the file for storing the serialized Items collection in the AppData folder.
        /// </summary>
        public static string CollectionFileName { get { return "SlideItemsCollection.xml"; } }

        // Commands
        /// <summary>
        /// The try again message.
        /// </summary>
        public const string TRY_AGAIN = "Try again";

        /// <summary>
        /// The close message.
        /// </summary>
        public const string CLOSE = "Close";
        
        // Image counts
        /// <summary>
        /// Gets the number of images to load incrementally each time LoadMoreFiles is called.
        /// </summary>
        public static int ImagesToLoad { get { return 12; } }

        /// <summary>
        /// Gets the number of the maximum number of image files to keep in the file list.
        /// </summary>
        public static int MaxCount { get { return 300; } }

        // Intervals
        /// <summary>
        /// Gets the array of intervals, in seconds, associated with each index of the combo box.
        /// </summary>
        public static int[] IndexList { get { return new int[] { 5, 10, 30, 60, 180 }; } }

        // Defaults
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
        /// Gets the decimal by which to scale the images when animating.
        /// </summary>
        public static double ScaleDecimal { get { return 1.25; } }
    }
}