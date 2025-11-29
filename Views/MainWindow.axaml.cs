using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Win32;
using PmGui.Gamepad;
using PmGui.Managers;
using PmGui.Models;
using PmGui.ViewModels;
using PmGui.ViewModels.Pages;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace PmGui.Views
{
    public partial class MainWindow : Window
    {
        private bool _isFirstShow = true;

        private GamepadNavigationManager _gamepadNav;

        public MainWindow()
        {
            InitializeComponent();
            ApplyWindowsScrollBarSetting();

            DataContext = new MainWindowViewModel();

            var viewModel = DataContext as MainWindowViewModel;

            // Pass ViewModel to gamepad navigator and don't auto-start
            _gamepadNav = new GamepadNavigationManager(this, viewModel, false);

            // Register this MainWindow and its ViewModel with GlobalAppManager
            GlobalAppManager.Instance.RegisterPageViewModel(this.DataContext as MainWindowViewModel);
            GlobalAppManager.Instance.MainViewModel = DataContext as MainWindowViewModel;

            // Center the window on screen load
            this.Activated += (sender, e) =>
            {
                this.Topmost = true;
                this.Topmost = false;

                if (_isFirstShow && this.WindowState != WindowState.Minimized)
                {
                    _isFirstShow = false;

                    var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
                    if (screen == null) return;

                    var scaling = screen.Scaling;

                    ApplyHighDpiUiScale(scaling);

                    // Desired window size in logical pixels
                    var desiredWidth = 1100;
                    var desiredHeight = 600;

                    // Set initial client size
                    var workingArea = screen.WorkingArea;
                    var availableWidthLogical = workingArea.Width / scaling;
                    var availableHeightLogical = workingArea.Height / scaling;
                    
                    this.ClientSize = new Size(
                        Math.Min(desiredWidth, availableWidthLogical * 0.9),
                        Math.Min(desiredHeight, availableHeightLogical * 0.9)
                    );

                    // Wait for the window to be fully rendered with a delay
                    var timer = new DispatcherTimer 
                    { 
                        Interval = TimeSpan.FromMilliseconds(100)
                    };
                    
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        
                        // Get the FRAME bounds which includes all window decorations
                        var frameSize = this.FrameSize;
                        
                        // If FrameSize is not available, calculate from bounds
                        if (frameSize == null || (frameSize.Value.Width == 0 && frameSize.Value.Height == 0))
                        {
                            // Use the difference between positioned window and client area
                            var currentPos = this.Position;
                            this.Position = new PixelPoint(0, 0); // Temporarily move to 0,0
                            
                            // Get bounds at 0,0
                            var windowBounds = this.Bounds;
                            
                            // Calculate the actual window size including all chrome
                            var totalWindowWidth = windowBounds.Width * scaling;
                            var totalWindowHeight = windowBounds.Height * scaling;
                            
                            // Calculate center position in the working area
                            var centerX = workingArea.X + ((workingArea.Width - totalWindowWidth) / 2.0);
                            var centerY = workingArea.Y + ((workingArea.Height - totalWindowHeight) / 2.0);
                            
                            this.Position = new PixelPoint((int)centerX, (int)centerY);
                        }
                        else
                        {
                            // Use frame size if available
                            var totalWindowWidth = frameSize.Value.Width * scaling;
                            var totalWindowHeight = frameSize.Value.Height * scaling;
                            
                            var centerX = workingArea.X + ((workingArea.Width - totalWindowWidth) / 2.0);
                            var centerY = workingArea.Y + ((workingArea.Height - totalWindowHeight) / 2.0);
                            
                            this.Position = new PixelPoint((int)centerX, (int)centerY);
                        }                        
                    };
                    
                    timer.Start();
                }
            };

            // Handle window closing - hide to system tray if enabled
            this.Closing += OnWindowClosing;

            // Handle window state changes (minimize button) - override OnPropertyChanged for WindowState
        }

        public GamepadNavigationManager  GamepadNav => _gamepadNav;

        public bool GamepadEnabled
        { 
            get => _gamepadNav.IsRunning; 
            set
            {
                if (value && GlobalSettings.Instance.UseGamepad)
                {
                    _gamepadNav?.Start();
                }
                else
                {
                    _gamepadNav?.Stop();
                }
            }
        }


        private void ApplyHighDpiUiScale(double scaling)
        {
            if (UiScaleRoot == null)
                return;

            // Limit UI scaling to 175%
            if (scaling > 1.75)
            {
                double targetVisualScale = 1.75;
                UiScaleRoot.Scale = targetVisualScale / scaling;

                this.Classes.Add("HighDpiMode");
            }
            else
            {
                UiScaleRoot.Scale = 1.0;
            }
        }

        private void ApplyWindowsScrollBarSetting()
        {
            if (ShouldAlwaysShowScrollbars())
            {
                // System: "Always show scrollbars" -> disable auto-hide in Avalonia
                this.Classes.Add("AlwaysShowScrollbars");
            }
            else
            {
                // System: allow scrollbars to auto-hide -> use default behavior
                this.Classes.Remove("AlwaysShowScrollbars");
            }
        }

        public static bool ShouldAlwaysShowScrollbars()
        {
            try
            {
                // Windows 10/11 dynamic scrollbars setting:
                // HKCU\Control Panel\Accessibility\DynamicScrollbars
                //
                // Typically:
                //   1 = dynamic (auto-hide)
                //   0 = always show
                var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility");
                if (key == null)
                    return false; // default: auto-hide

                var value = key.GetValue("DynamicScrollbars");

                if (value is int intVal)
                {
                    return intVal == 0;
                }

                if (value is string s && int.TryParse(s, out int parsed))
                {
                    return parsed == 0;
                }

                return false;
            }
            catch
            {
                // If anything goes wrong, fall back to normal auto-hide behavior.
                return false;
            }
        }


        /// <summary>
        /// Handles window closing event - hides window to system tray if the setting is enabled
        /// </summary>
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GamepadEnabled = false;

            if (GlobalAppManager.Instance.IsDebugMode)
            {
                Environment.Exit(0);
            }
            // Check if minimize to system tray is enabled
            var advancedPageViewModel = GlobalAppManager.Instance.GetPageViewModel<AdvancedPageViewModel>();
            if (advancedPageViewModel?.MinimizeToSystemTray == true)
            {
                // Cancel the close event and make the window invisible instead
                e.Cancel = true;
                this.IsVisible = false;
                GlobalAppManager.Instance.SendCmdIsVisible(false);
            }
            else
            {
                GlobalAppManager.Instance.SendCmdCloseApp(true);
                Thread.Sleep(500);
            }
        }
        /// <summary>
        /// Overridden to handle window state changes for minimize behavior
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Handle window state changes (minimize button)
            if (e.Property == Window.WindowStateProperty && e.NewValue is WindowState newState)
            {
                if (newState == WindowState.Minimized)
                {
                    GamepadEnabled = false;
                    // Check if minimize to system tray is enabled
                    var advancedPageViewModel = GlobalAppManager.Instance.GetPageViewModel<AdvancedPageViewModel>();
                    if (advancedPageViewModel?.MinimizeToSystemTray == true)
                    {
                        // Make the window invisible instead of keeping it minimized
                        // This allows it to be shown again later without the "Cannot re-show a closed window" error
                        this.IsVisible = false;
                    }
                }

                if (e.OldValue is WindowState oldState &&
                    (oldState == WindowState.Minimized || newState == WindowState.Minimized))
                {
                    GlobalAppManager.Instance.SendCmdIsVisible(newState != WindowState.Minimized);
                }
            }

            if (e.Property == Window.IsActiveProperty && e.NewValue is bool isActive)
            {
                GamepadEnabled = isActive;
            }
        }
    }
}