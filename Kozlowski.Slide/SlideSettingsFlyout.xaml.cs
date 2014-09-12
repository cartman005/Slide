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

            switch(tileNumber)
            {
                case 0:
                    tileId = "";
                    this.DataContext = Tile1Settings.Instance;
                    PinPanel.Visibility = Visibility.Collapsed;
                    Tile1Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                case 1:
                    tileId = Constants.Tile2Id;
                    this.DataContext = Tile2Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(tileId));
                    Tile2Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                case 2:
                    tileId = Constants.Tile3Id;
                    this.DataContext = Tile3Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(tileId));
                    Tile3Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                case 3:
                    tileId = Constants.Tile4Id;
                    this.DataContext = Tile4Settings.Instance;
                    TogglePinButton(!SecondaryTile.Exists(tileId));
                    Tile4Settings.Instance.PropertyChanged += Setting_Changed;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void Setting_Changed(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine("Settings Changed");

            if (e.PropertyName == Constants.SettingsName_Interval || e.PropertyName == Constants.SettingsName_ImagesLocation || e.PropertyName == Constants.SettingsName_Shuffle || e.PropertyName == Constants.SettingsName_Subfolders)
                CreateUpdates(tileNumber);
        }

        private async void CreateUpdates(int number)
        {
            var fileList = new List<StorageFile>();

            if (number == 0)
            {
                await TileMaker.GenerateTiles(Constants.Tile1SaveFolder, "", Tile1Settings.Instance.Interval, Tile1Settings.Instance.RootFolder, Tile1Settings.Instance.IncludeSubfolders, Tile1Settings.Instance.Shuffle, true);
                Tile1Settings.Instance.InitialUpdatesMade = true;
            }
            else
            {
                if (SecondaryTile.Exists(tileId))
                {
                    switch (tileNumber)
                    {
                        case 1:
                            await TileMaker.GenerateTiles(Constants.Tile2SaveFolder, Constants.Tile2Id, Tile2Settings.Instance.Interval, Tile2Settings.Instance.RootFolder, Tile2Settings.Instance.IncludeSubfolders, Tile2Settings.Instance.Shuffle, true);
                            Tile2Settings.Instance.InitialUpdatesMade = true;
                            break;
                        case 2:
                            await TileMaker.GenerateTiles(Constants.Tile3SaveFolder, Constants.Tile3Id, Tile3Settings.Instance.Interval, Tile3Settings.Instance.RootFolder, Tile3Settings.Instance.IncludeSubfolders, Tile3Settings.Instance.Shuffle, true);
                            Tile3Settings.Instance.InitialUpdatesMade = true;
                            break;
                        case 3:
                            await TileMaker.GenerateTiles(Constants.Tile4SaveFolder, Constants.Tile4Id, Tile4Settings.Instance.Interval, Tile4Settings.Instance.RootFolder, Tile4Settings.Instance.IncludeSubfolders, Tile4Settings.Instance.Shuffle, true);
                            Tile4Settings.Instance.InitialUpdatesMade = true;
                            break;
                    }
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
                        Tile2Settings.Instance.RootFolder = folder;
                        break;
                    case 2:
                        Tile3Settings.Instance.RootFolder = folder;
                        break;
                    case 3:
                        Tile4Settings.Instance.RootFolder = folder;
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
                var secondaryTile = new SecondaryTile(Constants.Tile2Id);

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
                                                        string.Format("Slide {0}", tileNumber + 1),
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
                    CreateUpdates(tileNumber);
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