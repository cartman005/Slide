
namespace Kozlowski.Slide.Shared
{
    public static class Constants
    {
        public static string TimerTaskName { get { return "SlideshowTimer"; } }
        public static string UserTaskName { get { return "SlideshowUserPresent"; } }
        public static string TaskEntry { get { return "Kozlowski.Slide.Background.TileUpdater"; } }

        public static int DefaultIntervalIndex { get { return 2; } }
        public static int[] IndexList { get { return new int[] { 5, 10, 30, 60, 180 }; } }
        public static string SettingsName { get { return "Interval"; } }
    }
}