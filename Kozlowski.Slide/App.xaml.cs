using Kozlowski.Slide.Common;
using Kozlowski.Slide.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace Kozlowski.Slide
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Stores the instance of Settings associated with this instance of the full screen slide show app.
        /// </summary>
        private Settings _settingsInstance;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Returns the Settings instance associated with this instance of the slide show.
        /// By default this is the main tile (tile 1) Settings instance, however if the app is launched through a secondary tile,
        /// it will be the Settings instance corresponding to that tile.
        /// </summary>
        public Settings Settings { get { return _settingsInstance; } }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            /*
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
            */
#endif
            // Set up Settings instance depending on how the app was launched
            if (string.IsNullOrEmpty(e.Arguments))
            {
                // App launched through the main tile
                _settingsInstance = Tile1Settings.Instance;
            }
            else
            {
                // App launched through a secondary tile
                switch (e.TileId)
                {
                    case Constants.TILE_2_ID:
                        _settingsInstance = Tile2Settings.Instance;
                        break;
                    case Constants.TILE_3_ID:
                        _settingsInstance = Tile3Settings.Instance;
                        break;
                    case Constants.TILE_4_ID:
                        _settingsInstance = Tile4Settings.Instance;
                        break;
                    default:
                        // Invalid tile ID
                        throw new NotImplementedException();
                }
            }
 
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                SuspensionManager.RegisterFrame(rootFrame, "appFrame");

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Restore the previous state only if the the app was suspended and then terminated
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    await SuspensionManager.RestoreAsync();
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Settings the OnCommandsRequested handler for the SettingsFlyouts.
        /// </summary>
        /// <param name="args">Unused parameter</param>
        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
        }

        /// <summary>
        /// Creates a SettingsFlyout for each tile when the Charms bar is opened.
        /// Secondary tile options will not be shown if background access is not permitted.
        /// </summary>
        /// <param name="sender">Unused parameter.</param>
        /// <param name="args">Unused parameter.</param>
        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // Show secondary tile options if background access is available
            var result = BackgroundExecutionManager.GetAccessStatus();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity || result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                // Main tile
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(Constants.Tile1Number), (handler) => ShowSettingsFlyout(Constants.Tile1Number)));
                
                // Secondary tiles
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(Constants.Tile2Number), (handler) => ShowSettingsFlyout(Constants.Tile2Number)));
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(Constants.Tile3Number), (handler) => ShowSettingsFlyout(Constants.Tile3Number)));
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(Constants.Tile4Number), (handler) => ShowSettingsFlyout(Constants.Tile4Number)));
            }
            else
                // Main tile
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", "Options", (handler) => ShowSettingsFlyout(Constants.Tile1Number)));
        }

        /// <summary>
        /// Helper function to the title of the SettingsFlyout according to the tile's number and if it is the flyout used to control the current set of images in the full screen slide show.
        /// </summary>
        /// <param name="tileNumber">The number of the tile to be formatted.</param>
        /// <returns>The formatted title of the SettingsFlyout.</returns>
        public string FormatOptionsTitle(int tileNumber)
        {
            if (this.Settings.TileNumber == tileNumber)
                return string.Format("Tile {0} Options (Current)", tileNumber);
            else
                return string.Format("Tile {0} Options", tileNumber);
        }

        /// <summary>
        /// Displays the SettingsFlyout for the specified tile number.
        /// </summary>
        /// <param name="tileNumber">The number of the tile whose settings will be displayed.</param>
        public void ShowSettingsFlyout(int tileNumber)
        {
            var settings = new SlideSettingsFlyout(tileNumber);
            settings.Show();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
