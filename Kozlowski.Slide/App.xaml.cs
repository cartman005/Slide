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
        private Settings settingsInstance;
        private int tileId;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        public Settings Settings { get { return settingsInstance; } }
        public int TileId { get { return tileId; } }

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
            // Set up settings instance
            if (string.IsNullOrEmpty(e.Arguments))
            {
                // Main tile
                settingsInstance = Tile1Settings.Instance;
                tileId = 0;
            }
            else
            {
                // Secondary tile
                switch (e.TileId)
                {
                    case "SlideSecondaryTile1":
                        settingsInstance = Tile2Settings.Instance;
                        tileId = 1;
                        break;
                    case "SlideSecondaryTile2":
                        settingsInstance = Tile3Settings.Instance;
                        tileId = 2;
                        break;
                    case "SlideSecondaryTile3":
                        settingsInstance = Tile4Settings.Instance;
                        tileId = 3;
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
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
        {
            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
        }

        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(0), (handler) => ShowSettingsFlyout(0)));

            var result = BackgroundExecutionManager.GetAccessStatus();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity || result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(1), (handler) => ShowSettingsFlyout(1)));
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(2), (handler) => ShowSettingsFlyout(2)));
                args.Request.ApplicationCommands.Add(new SettingsCommand("SlideOptions", FormatOptionsTitle(3), (handler) => ShowSettingsFlyout(3)));
            }
        }

        public string FormatOptionsTitle(int number)
        {
            if (tileId == number)
                return string.Format("Tile {0} Options (Current)", number + 1);
            else
                return string.Format("Tile {0} Options", number + 1);
        }

        public void ShowSettingsFlyout(int number)
        {
            var settings = new SlideSettingsFlyout(number);
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
