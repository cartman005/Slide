using Kozlowski.Slide.Background;
using Kozlowski.Slide.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace Kozlowski.Slide
{
    public sealed partial class SlideSettingsFlyout : SettingsFlyout
    {
        /// <summary>
        /// The number of the tile associated with this flyout.
        /// </summary>
        private int _tileNumber;

        /// <summary>
        /// The ID of the tile associated with this flyout.
        /// For use with secondary tiles.
        /// </summary>
        private string _tileId;

        /// <summary>
        /// Create the SettingsFlyout for the specified tile number.
        /// </summary>
        /// <param name="tileNumber">The number of the tile to be represented by these settings.</param>
        public SlideSettingsFlyout(int tileNumber)
        {
            _tileNumber = tileNumber;

            this.InitializeComponent();
            this.Title = string.Format("Slide Tile {0}", _tileNumber);

            switch(_tileNumber)
            {
                case Constants.TILE_1_NUMBER:
                    _tileId = Constants.Tile1Id;
                    this.DataContext = Tile1Settings.Instance;
                    PinPanel.Visibility = Visibility.Collapsed;
                    Tile1Settings.Instance.PropertyChanged += Settings_Changed;
                    break;
                case Constants.TILE_2_NUMBER:
                    _tileId = Constants.Tile2Id;
                    this.DataContext = Tile2Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(_tileId));
                    Tile2Settings.Instance.PropertyChanged += Settings_Changed;
                    break;
                case Constants.TILE_3_NUMBER:
                    _tileId = Constants.Tile3Id;
                    this.DataContext = Tile3Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(_tileId));
                    Tile3Settings.Instance.PropertyChanged += Settings_Changed;
                    break;
                case Constants.TILE_4_NUMBER:
                    _tileId = Constants.Tile4Id;
                    this.DataContext = Tile4Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(_tileId));
                    Tile4Settings.Instance.PropertyChanged += Settings_Changed;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Re-creates background tile updates when settings affecting them are changed.
        /// This handler does not deal with the full screen slidesow. The MainPage's handler is responsible for those changes.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="e">Contains details on the property that was changed.</param>
        private async void Settings_Changed(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Settings Changed in SettingsFlyout");

            if (e.PropertyName == Constants.SettingsName_Interval || e.PropertyName == Constants.SettingsName_ImagesLocation || e.PropertyName == Constants.SettingsName_Shuffle || e.PropertyName == Constants.SettingsName_Subfolders)
            {
                if (SecondaryTile.Exists(_tileId))
                    await TileMaker.CreateTileUpdates(_tileNumber, true);
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
            folderPicker.SettingsIdentifier = _tileId;

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                switch (_tileNumber)
                {
                    case Constants.TILE_1_NUMBER:
                        Tile1Settings.Instance.RootFolder = folder;
                        break;
                    case Constants.TILE_2_NUMBER:
                        Tile2Settings.Instance.RootFolder = folder;
                        break;
                    case Constants.TILE_3_NUMBER:
                        Tile3Settings.Instance.RootFolder = folder;
                        break;
                    case Constants.TILE_4_NUMBER:
                        Tile4Settings.Instance.RootFolder = folder;
                        break;
                }
            }
        }

        private async void Pin_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pin click");

            if (SecondaryTile.Exists(_tileId))
            {
                // Unpin
                var secondaryTile = new SecondaryTile(Constants.Tile2Id);

                var rect = GetElementRect((FrameworkElement)sender);
                var placement = Windows.UI.Popups.Placement.Above;

                var unpinned = await secondaryTile.RequestDeleteForSelectionAsync(rect, placement);
                if (unpinned)
                    TogglePinButton(false);

                // TODO Delete files and updates?
            }
            else
            {
                // Pin
                var logo = new Uri("ms-appx:///Assets/Logo.png");
                var tileActivationArguments = string.Format("{0} was pinned at = {1}", _tileId, DateTime.Now.ToLocalTime().ToString());

                var newTileDesiredSize = TileSize.Square150x150;

                var secondaryTile = new SecondaryTile(_tileId,
                                                        string.Format("Slide {0}", _tileNumber),
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
                    Debug.WriteLine("Update UI");
                    TogglePinButton(true);
                    Debug.WriteLine("UI updated");
                    await TileMaker.CreateTileUpdates(_tileNumber, true);
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