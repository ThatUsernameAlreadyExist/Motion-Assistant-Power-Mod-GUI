using Avalonia.Media;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Windows11Settings.Managers;
using Windows11Settings.Resources.Localization;

namespace Windows11Settings.ViewModels
{
    public class MonitoringPageViewModel : INotifyPropertyChanged
    {
        private readonly LocalizationManager _localization;
        private double _packagePower = 0;
        private double _packagePowerMax = 65;
        private double _cpuTemperature = 0;
        private double _cpuTemperatureMax = 100;
        private double _cpuUsage = 0;
        private double _gpuUsage = 0;
        private double _fanSpeed = 0;
        private double _fanSpeedMax = 6000;
        private double _batteryPowerMax = 95;
        private double _batteryPower = 0;
         private string _tdpLimit = "0";
        private uint _gpuLockClock = 0;
        private string _cpuBoost = "??";
        private Color _cpuBoostColor = Colors.Green;

        public Color PackagePowerColor => GetGradientColor(PackagePowerPercentage, 80.0, Colors.White, Colors.DarkOrange);
        public Color CpuTemperatureColor => GetGradientColor(CpuTemperaturePercentage, 100.0, Colors.White, Colors.Red);
        public Color CpuUsageColor => GetGradientColor(CpuUsage, 80.0, Colors.White, Colors.RoyalBlue);
        public Color GpuUsageColor => GetGradientColor(GpuUsage, 70.0, Colors.White, Colors.DarkOrchid);
        public Color FanSpeedColor => GetGradientColor(FanSpeedPercentage, 100.0, Colors.White, Colors.DeepSkyBlue);
        public Color BatteryPowerColor => GetGradientColor(BatteryPowerPercentage, 80.0, Colors.White, Colors.Green);

        public MonitoringPageViewModel()
        {
            _localization = LocalizationManager.Instance;
            _localization.PropertyChanged += (s, e) => RefreshTranslations();
            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        private void RefreshTranslations()
        {
            OnPropertyChanged(nameof(CPUBoostTranslated));
        }

        private Color GetGradientColor(double percentage, double percentageLimit, Color start, Color end)
        {
            var pl = percentageLimit > percentage ? 100.0 - percentageLimit + percentage : 100.0;
            var p = pl / 100.0;
            var r = (byte)(start.R + p * (end.R - start.R));
            var g = (byte)(start.G + p * (end.G - start.G));
            var b = (byte)(start.B + p * (end.B - start.B));
            return Color.FromRgb(r, g, b);
        }

        public string TDPLimit
        {
            get => _tdpLimit;
            set
            {
                if (_tdpLimit != value)
                {
                    _tdpLimit = value;
                    OnPropertyChanged();
                }
            }
        }

        public uint GPULockClock
        {
            get => _gpuLockClock;
            set
            {
                if (_gpuLockClock != value)
                {
                    _gpuLockClock = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CPUBoost
        {
            get => _cpuBoost;
            set
            {
                if (_cpuBoost != value)
                {
                    _cpuBoost = value;
                    CPUBoostColor = value == "On" || value == "1" ? Colors.Green : Colors.Red;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CPUBoostTranslated));
                    OnPropertyChanged(nameof(CPUBoostColor));
                }
            }
        }

        public Color CPUBoostColor
        {
            get => _cpuBoostColor;
            set
            {
                if (_cpuBoostColor != value)
                {
                    _cpuBoostColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CPUBoostTranslated
        {
            get => _localization[_cpuBoost == "On" || _cpuBoost == "1" ? "On" : "Off"] ?? _cpuBoost;
        }

        // Package Power
        public double PackagePower
        {
            get => _packagePower;
            set
            {
                if (Math.Abs(_packagePower - value) > 0.01)
                {
                    _packagePower = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PackagePowerPercentage));
                    OnPropertyChanged(nameof(PackagePowerArc));
                    OnPropertyChanged(nameof(PackagePowerColor));
                    OnPropertyChanged(nameof(PackagePowerFormatted));
                }
            }
        }

        public string PackagePowerFormatted => PackagePower > 0 && PackagePower < 10 ? PackagePower.ToString("F1") : PackagePower.ToString("F0");

        public double PackagePowerMax
        {
            get => _packagePowerMax;
            set
            {
                if (Math.Abs(_packagePowerMax - value) > 0.01)
                {
                    _packagePowerMax = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PackagePowerPercentage));
                    OnPropertyChanged(nameof(PackagePowerArc));
                    OnPropertyChanged(nameof(PackagePowerColor));
                }
            }
        }


        public double PackagePowerPercentage => (PackagePower / PackagePowerMax) * 100;
        public string PackagePowerArc => CalculateArcPath(PackagePowerPercentage);

        // CPU Temperature
        public double CpuTemperature
        {
            get => _cpuTemperature;
            set
            {
                if (Math.Abs(_cpuTemperature - value) > 0.01)
                {
                    _cpuTemperature = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CpuTemperaturePercentage));
                    OnPropertyChanged(nameof(CpuTemperatureArc));
                    OnPropertyChanged(nameof(CpuTemperatureColor));
                }
            }
        }

        public double CpuTemperatureMax
        {
            get => _cpuTemperatureMax;
            set
            {
                if (Math.Abs(_cpuTemperatureMax - value) > 0.01)
                {
                    _cpuTemperatureMax = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CpuTemperaturePercentage));
                    OnPropertyChanged(nameof(CpuTemperatureArc));
                    OnPropertyChanged(nameof(CpuTemperatureColor));
                }
            }
        }

        public double CpuTemperaturePercentage => (CpuTemperature / CpuTemperatureMax) * 100;
        public string CpuTemperatureArc => CalculateArcPath(CpuTemperaturePercentage);

        // CPU Usage
        public double CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (Math.Abs(_cpuUsage - value) > 0.01)
                {
                    _cpuUsage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CpuUsageArc));
                    OnPropertyChanged(nameof(CpuUsageColor));
                }
            }
        }

        public string CpuUsageArc => CalculateArcPath(CpuUsage);

        // GPU Usage
        public double GpuUsage
        {
            get => _gpuUsage;
            set
            {
                if (Math.Abs(_gpuUsage - value) > 0.01)
                {
                    _gpuUsage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GpuUsageArc));
                    OnPropertyChanged(nameof(GpuUsageColor));
                }
            }
        }

        public string GpuUsageArc => CalculateArcPath(GpuUsage);

        // Fan Speed

        public double FanSpeedDivided
        {
            get => _fanSpeed / 10.0;
            set => _fanSpeed = value * 10.0;
        }

        public double FanSpeed
        {
            get => _fanSpeed;
            set
            {
                if (Math.Abs(_fanSpeed - value) > 0.01)
                {
                    _fanSpeed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FanSpeedPercentage));
                    OnPropertyChanged(nameof(FanSpeedArc));
                    OnPropertyChanged(nameof(FanSpeedColor));
                }
            }
        }

        public double FanSpeedMax
        {
            get => _fanSpeedMax;
            set
            {
                if (Math.Abs(_fanSpeedMax - value) > 0.01)
                {
                    _fanSpeedMax = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FanSpeedPercentage));
                    OnPropertyChanged(nameof(FanSpeedArc));
                    OnPropertyChanged(nameof(FanSpeedColor));
                }
            }
        }

        public double FanSpeedPercentage => (FanSpeed / FanSpeedMax) * 100;
        public string FanSpeedArc => CalculateArcPath(FanSpeedPercentage);

        // Battery Charge
        public double BatteryPower
        {
            get => _batteryPower;
            set
            {
                if (Math.Abs(_batteryPower - value) > 0.01)
                {
                    _batteryPower = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BatteryPowerPercentage));
                    OnPropertyChanged(nameof(BatteryPowerArc));
                    OnPropertyChanged(nameof(BatteryPowerColor));
                    OnPropertyChanged(nameof(BatteryPowerFormatted));
                }
            }
        }

        public string BatteryPowerFormatted => BatteryPower > 0 && BatteryPower < 10 ? BatteryPower.ToString("F1") : BatteryPower.ToString("F0");

        public double BatteryPowerMax
        {
            get => _batteryPowerMax;
            set
            {
                if (Math.Abs(_batteryPowerMax - value) > 0.01)
                {
                    _batteryPowerMax = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BatteryPowerPercentage));
                    OnPropertyChanged(nameof(BatteryPowerArc));
                    OnPropertyChanged(nameof(BatteryPowerColor));
                }
            }
        }

        public string BatteryPowerArc => CalculateArcPath(BatteryPowerPercentage);
        public double BatteryPowerPercentage => (BatteryPower / BatteryPowerMax) * 100;

        // Calculate arc path based on percentage (0-100)
        private string CalculateArcPath(double percentage)
        {
            // Clamp percentage between 0 and 100
            percentage = Math.Max(0, Math.Min(100, percentage));

            // Starting point
            double startX = 20;
            double startY = 70;

            // Center of the arc circle
            double centerX = 70;
            double centerY = 70;
            double radius = 50;

            // The arc goes from 180° to 0° (semicircle)
            double angleRadians = Math.PI - (percentage / 100.0 * Math.PI);

            // Calculate end point
            double endX = centerX + radius * Math.Cos(angleRadians);
            double endY = centerY - radius * Math.Sin(angleRadians);

            // THIS IS THE FIX: For an arc under 180 degrees, the large-arc-flag should always be 0.
            int largeArcFlag = 0;

            // Use invariant culture to ensure decimal point (not comma)
            return string.Format(CultureInfo.InvariantCulture,
                "M {0:F1},{1:F1} A {2:F1},{2:F1} 0 {3} 1 {4:F1},{5:F1}",
                startX, startY, radius, largeArcFlag, endX, endY);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}