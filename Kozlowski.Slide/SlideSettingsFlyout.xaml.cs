using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Kozlowski.Slide
{
    public sealed partial class SlideSettingsFlyout : SettingsFlyout
    {
        /// <summary>
        /// Create the Settings flyout.
        /// </summary>
        public SlideSettingsFlyout()
        {
            this.InitializeComponent();
            this.DataContext = Settings.Instance;
        }

        /// <summary>
        /// Displays a FolderPicker for image file types, starting at the Pictures Library.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="e">Unused parameter.</param>
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
                Settings.Instance.RootFolder = folder;
        }

    }
}
