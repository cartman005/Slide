using Kozlowski.Slide.Background;
using Kozlowski.Slide.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Kozlowski.Slide
{
    public sealed partial class SlideSettingsFlyout : SettingsFlyout
    {
        private int tileNumber;
        private string tileId;

        /// <summary>
        /// Create the Settings flyout.
        /// </summary>
        public SlideSettingsFlyout(int number)
        {
            tileNumber = number;

            this.InitializeComponent();
            this.Title = string.Format("Slide Tile {0}", number + 1);

            if (number == 0)
                PinPanel.Visibility = Visibility.Collapsed;

            switch(tileNumber)
            {
                case 1:
                    tileId = Constants.Secondary1TileId;
                    this.DataContext = Secondary1Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(tileId));
                    Secondary1Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                case 2:
                    tileId = Constants.Secondary2TileId;
                    this.DataContext = Secondary2Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(tileId));
                    Secondary2Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                case 3:
                    tileId = Constants.Secondary3TileId;
                    this.DataContext = Secondary3Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(tileId));
                    Secondary3Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void Setting_Changed(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Settings Changed");

            CreateUpdates();
        }

        private async void CreateUpdates()
        {
            var fileList = new List<StorageFile>();

            if (SecondaryTile.Exists(tileId))
            {
                switch (tileNumber)
                {
                    case 1:
                        fileList.AddRange(await TileMaker.GetImageList(Secondary1Settings.Instance.RootFolder, Secondary1Settings.Instance.IncludeSubfolders, Secondary1Settings.Instance.Shuffle));
                        await TileMaker.GenerateTiles(Constants.Secondary1TileUpdatesFolder, Constants.Secondary1TileId, Secondary1Settings.Instance.Interval, fileList, Secondary1Settings.Instance.RootFolder, Secondary1Settings.Instance.IncludeSubfolders, Secondary1Settings.Instance.Shuffle);
                        Secondary1Settings.Instance.InitialUpdatesMade = true;
                        break;
                    case 2:
                        fileList.AddRange(await TileMaker.GetImageList(Secondary2Settings.Instance.RootFolder, Secondary2Settings.Instance.IncludeSubfolders, Secondary2Settings.Instance.Shuffle));
                        await TileMaker.GenerateTiles(Constants.Secondary2TileUpdatesFolder, Constants.Secondary2TileId, Secondary2Settings.Instance.Interval, fileList, Secondary2Settings.Instance.RootFolder, Secondary2Settings.Instance.IncludeSubfolders, Secondary2Settings.Instance.Shuffle);
                        Secondary2Settings.Instance.InitialUpdatesMade = true;
                        break;
                    case 3:
                        fileList.AddRange(await TileMaker.GetImageList(Secondary3Settings.Instance.RootFolder, Secondary3Settings.Instance.IncludeSubfolders, Secondary3Settings.Instance.Shuffle));
                        await TileMaker.GenerateTiles(Constants.Secondary3TileUpdatesFolder, Constants.Secondary3TileId, Secondary3Settings.Instance.Interval, fileList, Secondary3Settings.Instance.RootFolder, Secondary3Settings.Instance.IncludeSubfolders, Secondary3Settings.Instance.Shuffle);
                        Secondary3Settings.Instance.InitialUpdatesMade = true;
                        break;
                }
            }
            
        }

        private void TogglePinButton(bool showPinButton)
        {
            var toolTip = new ToolTip();

            if (showPinButton)
            {
                Pin.Content = "Pin to Start";
                toolTip.Content = "Pin this tile to the Start screen";
            }
            else
            {
                Pin.Content = "Unpin from Start";
                toolTip.Content = "Remove this tile from the Start screen";
            }

            ToolTipService.SetToolTip(Pin, toolTip);
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
            {
                switch (tileNumber)
                {
                    case 1:
                        Secondary1Settings.Instance.RootFolder = folder;
                        break;
                    case 2:
                        Secondary2Settings.Instance.RootFolder = folder;
                        break;
                    case 3:
                        Secondary3Settings.Instance.RootFolder = folder;
                        break;
                }
            }
        }

        private async void Pin_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pin click");

            if (SecondaryTile.Exists(tileId))
            {
                // Unpin
                var secondaryTile = new SecondaryTile(Constants.Secondary1TileId);

                var rect = GetElementRect((FrameworkElement)sender);
                var placement = Windows.UI.Popups.Placement.Above;

                await secondaryTile.RequestDeleteForSelectionAsync(rect, placement);

                // TODO Delete files and updates?
            }
            else
            {
                // Pin
                var logo = new Uri("ms-appx:///Assets/Logo.png");
                var tileActivationArguments = tileId + " was pinned at = " + DateTime.Now.ToLocalTime().ToString();

                var newTileDesiredSize = TileSize.Square150x150;

                var secondaryTile = new SecondaryTile(tileId,
                                                        "Slide Secondary " + tileNumber,
                                                        tileActivationArguments,
                                                        logo,
                                                        newTileDesiredSize);

                secondaryTile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Logo.png");
                secondaryTile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Logo.png");
                secondaryTile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/Square310x310Logo.png");
                
                // Doesn't make sense to have tile roam due to file path
                secondaryTile.RoamingEnabled = false;
                                
                var pinned = await secondaryTile.RequestCreateAsync();

                if (pinned)
                {
                    CreateUpdates();
                }
            }
        }

        private Rect GetElementRect(FrameworkElement element)
        {
            var buttonTransform = element.TransformToVisual(null);
            var point = buttonTransform.TransformPoint(new Point());

            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }
    }
}
