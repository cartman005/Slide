
namespace BackgroundSlideshow
{
    public static class Constants
    {

        public static string TaskName { get { return "SlideshowTimer"; } }
        public static string TaskEntry { get { return "BackgroundSlideshow.TileUpdater"; } }
        public static int ListLimit { get { return 100; } }
        public static int DefaultIntervalIndex { get { return 2; } }
        public static int[] IndexList { get { return new int[] { 5, 10, 30, 60, 180 }; } }
        public static string SettingsName { get { return "Interval"; } }
    }
}