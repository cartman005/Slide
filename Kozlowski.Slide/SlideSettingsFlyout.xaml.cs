using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Kozlowski.Slide
{
    public sealed partial class SlideSettingsFlyout : SettingsFlyout
    {
        private Settings settings;

        public SlideSettingsFlyout()
        {
            this.InitializeComponent();
            settings = Settings.Instance;
            this.DataContext = settings;
        }

        private async void BrowseForRootFolder(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();

            folderPicker.ViewMode = PickerViewMode.List;
            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.FileTypeFilter.Add(".bmp");
            folderPicker.FileTypeFilter.Add(".gif");
            folderPicker.FileTypeFilter.Add(".jpg");
            folderPicker.FileTypeFilter.Add(".jpeg");
            folderPicker.FileTypeFilter.Add(".png");
            folderPicker.FileTypeFilter.Add(".tiff");
            folderPicker.SettingsIdentifier = "FolderPicker";

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
                settings.RootFolder = folder;
        }

    }
}
