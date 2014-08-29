
namespace Kozlowski.Slide.Shared
{
    public static class Constants
    {
        public static string TimerTaskName { get { return "SlideTimer"; } }
        public static string UserTaskName { get { return "SlideUserPresent"; } }
        public static string TaskEntry { get { return "Kozlowski.Slide.Background.TileUpdater"; } }
        public static string SettingsName { get { return "Interval"; } }
        public static string CollectionFileName { get { return "SlideItemsCollection.xml"; } }
        public static int ImagesToLoad { get { return 10; } }
        public static int MaxCount { get { return 200; } }
        public static int DefaultIntervalIndex { get { return 2; } }
        public static int[] IndexList { get { return new int[] { 5, 10, 30, 60, 180 }; } }
        public static int BackgroundTaskInterval { get { return 15; } }
    }
}