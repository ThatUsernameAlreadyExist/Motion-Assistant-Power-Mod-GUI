using Avalonia;
using Avalonia.Controls;
using System;
using PmGui.Managers;
using PmGui.ViewModels;

namespace PmGui.Views.Pages
{
    public partial class MonitoringPage : UserControl
    {
        public MonitoringPageViewModel ViewModel { get; }

        public MonitoringPage()
        {
            ViewModel = new MonitoringPageViewModel();
            InitializeComponent();

            // Start live simulation to see values change
            if (GlobalAppManager.Instance.IsDebugMode)
            {
                StartSimulation();
            }

            this.GetObservable(IsVisibleProperty).Subscribe(OnIsVisibleChanged);
        }

        private void OnIsVisibleChanged(bool obj)
        {
            GlobalAppManager.Instance.SendCmdIsMonitoringVisible(obj);
            // Pause / resume animations when the page is shown / hidden (Fix 5).
            // While paused the shared DispatcherTimer is stopped → zero CPU.
            AnimatedDouble.SetPaused(!obj);
        }

        private async void StartSimulation()
        {
            var random = new System.Random();
            
            // Wait a bit for UI to initialize
            await System.Threading.Tasks.Task.Delay(500);

            ViewModel.BatteryPowerMax = 65;
            ViewModel.PackagePowerMax = 35;

            while (true)
            {
                await System.Threading.Tasks.Task.Delay(2000);

                ViewModel.PackagePower = random.Next(20, 60);
                ViewModel.CpuTemperature = random.Next(40, 85);
                ViewModel.CpuUsage = random.Next(10, 95);
                ViewModel.GpuUsage = random.Next(20, 90);
                ViewModel.FanSpeed = random.Next(1000, 2800);
                ViewModel.BatteryPower = random.Next(20, 100);
            }
        }
    }
}
