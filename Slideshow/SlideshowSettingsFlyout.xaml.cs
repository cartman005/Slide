using Kozlowski.Slideshow.Background;
using Kozlowski.Slideshow.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Kozlowski.Slideshow
{
    public sealed partial class SlideshowSettingsFlyout : SettingsFlyout
    {
        private Settings settings;

        public SlideshowSettingsFlyout()
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
