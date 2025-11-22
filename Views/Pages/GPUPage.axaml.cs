using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Windows11Settings.Resources.Localization;
using Windows11Settings.ViewModels;
using Windows11Settings.Managers;
using System;
using Windows11Settings.ViewModels.Pages;
using System.Reflection;

namespace Windows11Settings.Views.Pages
{
    public partial class GPUPage : UserControl
    {
        public GPUPage()
        {
            InitializeComponent();
            
            // Subscribe to localization changes
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
        }

        private void OnLocalizationChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Refresh view model when language changes
            if (DataContext is GPUPageViewModel viewModel)
            {
                viewModel.RefreshTranslations();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Initialize view model if not already set through parent window
            if (DataContext == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    DataContext = mainWindowVm.GPUPageViewModel;
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            
            // Unsubscribe from localization changes
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
        }

        private void MinGpuClockSlider_DragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider)
            {
                // Get the main window view model using Application.Current pattern like FanPage
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                    {
                        var gpuVm = mainWindowVm.GPUPageViewModel;
                        if (gpuVm != null)
                        {
                            GlobalAppManager.Instance.SendCmdMinGpuClockValue((int)slider.Value);
                            GlobalAppManager.Instance.SendCmdMaxGpuClockValue((int)gpuVm.MaxGpuClockValue);
                        }
                    }
                }
            }
        }

        private void MaxGpuClockSlider_DragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider)
            {
                // Get the main window view model using Application.Current pattern like FanPage
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var mainWindow = desktop.MainWindow;
                    if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                    {
                        var gpuVm = mainWindowVm.GPUPageViewModel;
                        if (gpuVm != null)
                        {
                            GlobalAppManager.Instance.SendCmdMaxGpuClockValue((int)slider.Value);
                            GlobalAppManager.Instance.SendCmdMinGpuClockValue((int)gpuVm.MinGpuClockValue);
                        }
                    }
                }
            }
        }
    }
}
