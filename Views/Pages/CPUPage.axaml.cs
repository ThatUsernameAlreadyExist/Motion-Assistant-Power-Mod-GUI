using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Windows11Settings.ViewModels;
using Windows11Settings.Managers;
using System;
using System.Reflection;

namespace Windows11Settings.Views.Pages
{
    public partial class CPUPage : UserControl
    {
        public CPUPage()
        {
            InitializeComponent();
        }

        private void OnPowerlineTdpDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
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
                        var cpuVm = mainWindowVm.CPUPageViewModel;
                        if (cpuVm != null)
                        {
                            GlobalAppManager.Instance.SendCmdPowerLineTdpValue((int)slider.Value);
                        }
                    }
                }
            }
        }

        private void OnBatteryTdpDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
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
                        var cpuVm = mainWindowVm.CPUPageViewModel;
                        if (cpuVm != null)
                        {
                            GlobalAppManager.Instance.SendCmdBatteryTdpValue((int)slider.Value);
                        }
                    }
                }
            }
        }
    }
}