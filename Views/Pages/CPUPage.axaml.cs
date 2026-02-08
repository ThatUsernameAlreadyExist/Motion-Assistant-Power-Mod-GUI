using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PmGui.ViewModels;
using PmGui.Managers;
using System;
using System.Reflection;
using Avalonia.Threading;

namespace PmGui.Views.Pages
{
    public partial class CPUPage : UserControl
    {
        public CPUPage()
        {
            InitializeComponent();
        }

        private IDisposable _debounceTimerPowerline;
        private IDisposable _debounceTimerBattery;

        private void SendPowerlineTdpValue(int value)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    var cpuVm = mainWindowVm.CPUPageViewModel;
                    if (cpuVm != null)
                    {
                        GlobalAppManager.Instance.SendCmdPowerLineTdpValue(value);
                    }
                }
            }
        }

        private void SendBatteryTdpValue(int value)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    var cpuVm = mainWindowVm.CPUPageViewModel;
                    if (cpuVm != null)
                    {
                        GlobalAppManager.Instance.SendCmdBatteryTdpValue(value);
                    }
                }
            }
        }

        private void OnPowerlineTdpDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");

            if (sender is Slider slider)
            {
                var currentValue = (int)slider.Value;

                // Cancel any existing timer
                _debounceTimerPowerline?.Dispose();
                _debounceTimerPowerline = null;


                // For ValueChanged events, use 1-second debounce with captured value
                _debounceTimerPowerline = DispatcherTimer.RunOnce(() =>
                {
                    SendPowerlineTdpValue(currentValue);
                    _debounceTimerPowerline = null;
                }, TimeSpan.FromSeconds(1));
            }
        }

        private void OnBatteryTdpDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");

            if (sender is Slider slider)
            {
                var currentValue = (int)slider.Value;

                // Cancel any existing timer
                _debounceTimerBattery?.Dispose();
                _debounceTimerBattery = null;


                // For ValueChanged events, use 1-second debounce with captured value
                _debounceTimerBattery = DispatcherTimer.RunOnce(() =>
                {
                    SendBatteryTdpValue(currentValue);
                    _debounceTimerBattery = null;
                }, TimeSpan.FromSeconds(1));
            }
        }
    }
}