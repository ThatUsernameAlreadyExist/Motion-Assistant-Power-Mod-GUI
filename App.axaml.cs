using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using PmGui.Gamepad;
using PmGui.Managers;
using PmGui.Models;
using PmGui.Views;

namespace PmGui
{
    public partial class App : Application
    {
        private bool _isHiddenMode = false;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // Check if we're in hidden mode
            _isHiddenMode = Environment.GetEnvironmentVariable("MAPM_031125_HIDDEN_MODE") == "true";

            // Initialize GlobalSettings at startup - this will load pmgui.ini and apply settings
            var settings = GlobalSettings.Instance;

            // Register this App instance with the GlobalAppManager
            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Don't set MainWindow initially - we'll show it when needed
            // Set shutdown mode to manual shutdown so the app doesn't close when no windows are shown
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            }

            base.OnFrameworkInitializationCompleted();

            // If we're in debug mode (normal mode), show the window immediately
            if (!_isHiddenMode)
            {
                NormalizeWindow();
                ShowMainWindow();
            }
        }

        /// <summary>
        /// Shows the main window when called
        /// </summary>
        public void ShowMainWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Create the main window if it doesn't exist
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                // Show the window
                if (desktop.MainWindow.WindowState != WindowState.Minimized)
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                    desktop.MainWindow.BringIntoView();  // Ensures window is visible and in view
                    desktop.MainWindow.Focus();

                    if (desktop.MainWindow is MainWindow mainWindow)
                    {
                        // Set initial navigation context to menu
                        mainWindow.GamepadNav.SetNavigationContext(NavigationContext.Menu);
                    }

                    GlobalAppManager.Instance.SendCmdIsVisible(true);
                }
            }
        }

        /// <summary>
        /// Hides the main window when called
        /// </summary>
        public void HideMainWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                desktop.MainWindow.Hide();
            }
        }

        public void MinimizeWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                desktop.MainWindow.WindowState = WindowState.Minimized;
            }
        }

        /// <summary>
        /// Maximizes or restores the main window
        /// </summary>
        public void MaximizeWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                desktop.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        public void NormalizeWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                desktop.MainWindow.WindowState = WindowState.Normal;
            }
        }
    }
}