
namespace Kozlowski.Slide.Shared
{
    public static class Constants
    {
        // Background tasks
        public static string TimerTaskName { get { return "SlideTimer"; } }
        public static string UserTaskName { get { return "SlideUserPresent"; } }
        public static string TaskEntryPoint { get { return "Kozlowski.Slide.Background.TileUpdater"; } }

        // Settings names
        // Persistent
        public static string SettingsName_Interval { get { return "Interval"; } }
        public static string SettingsName_InitialUpdatesMade { get { return "InitialUpdatesMade"; } }
        public static string SettingsName_ImagesLocation { get { return "FolderPath"; } }
        public static string SettingsName_Shuffle { get { return "Shuffle"; } }
        public static string SettingsName_Subfolders { get { return "IncludeSubfolders"; } }
        public static string SettingsName_Animate { get { return "Animate"; } }
        // Temporary
        public static string SettingsName_SelectedIndex { get { return "SelectedIndex"; } }
        public static string SettingsName_IsPaused { get { return "IsPaused"; } }

        // File names
        public static string TileUpdatesFolder { get { return "TileUpdates"; } }
        public static string CollectionFileName { get { return "SlideItemsCollection.xml"; } }
        
        // Image counts
        public static int ImagesToLoad { get { return 10; } }
        public static int MaxCount { get { return 200; } }

        // Intervals
        public static int DefaultIntervalIndex { get { return 2; } }
        public static int[] IndexList { get { return new int[] { 5, 10, 30, 60, 180 }; } }
        public static int BackgroundTaskInterval { get { return 15; } }
        public static double Scale { get { return 1.25; } }
    }
}