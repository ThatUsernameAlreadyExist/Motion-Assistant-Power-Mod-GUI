using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using PmGui.Gamepad;
using PmGui.Helpers;
using PmGui.Managers;
using PmGui.Models;
using PmGui.Views;
using System;

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

                var mainWindow = desktop.MainWindow;

                mainWindow.Show();
                mainWindow.BringIntoView();

                // Get native handle and force foreground
                IntPtr handle = WindowFocusHelper.GetHandle(mainWindow);

                if (handle != IntPtr.Zero)
                {
                    // Use Windows API to force foreground
                    bool success = WindowFocusHelper.ForceForeground(handle);

                    if (!success)
                    {
                        // Fallback: flash the window to get user attention
                        WindowFocusHelper.FlashWindow(handle);
                    }
                }

                // Avalonia focus operations
                mainWindow.Activate();
                mainWindow.Focus();


                // Set navigation context if MainWindow
                if (mainWindow is MainWindow mw)
                {
                    mw.GamepadNav.SetNavigationContext(NavigationContext.Menu);
                    mw.GamepadEnabled = true;
                }

                GlobalAppManager.Instance.SendCmdIsVisible(true);

                PowerEfficiency.SetEcoQoS(false);
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