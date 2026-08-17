using Avalonia.Media;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using PmGui.Managers;
using PmGui.Resources.Localization;

namespace PmGui.ViewModels
{
    public class MonitoringPageViewModel : INotifyPropertyChanged
    {
        private readonly LocalizationManager _localization;

        // Animated gauge values
        private readonly AnimatedDouble _packagePowerAnimated;
        private readonly AnimatedDouble _cpuTemperatureAnimated;
        private readonly AnimatedDouble _cpuUsageAnimated;
        private readonly AnimatedDouble _gpuUsageAnimated;
        private readonly AnimatedDouble _fanSpeedAnimated;
        private readonly AnimatedDouble _batteryPowerAnimated;

        // Max values (not animated — they change only when hardware reports new limits)
        private double _packagePowerMax = 65;
        private double _cpuTemperatureMax = 100;
        private double _fanSpeedMax = 6000;
        private double _batteryPowerMax = 95;

        private string _tdpLimit = "0";
        private uint _gpuLockClock = 0;
        private string _cpuBoost = "?";
        private Color _cpuBoostColor = Colors.Green;

        private string _lastPackagePowerArc;
        private Color _lastPackagePowerColor;
        private string _lastPackagePowerFormatted;

        private string _lastCpuTemperatureArc;
        private Color _lastCpuTemperatureColor;
        private string _lastCpuTemperatureFormatted;

        private string _lastCpuUsageArc;
        private Color _lastCpuUsageColor;
        private string _lastCpuUsageFormatted;

        private string _lastGpuUsageArc;
        private Color _lastGpuUsageColor;
        private string _lastGpuUsageFormatted;

        private string _lastFanSpeedArc;
        private Color _lastFanSpeedColor;
        private string _lastFanSpeedFormatted;

        private string _lastBatteryPowerArc;
        private Color _lastBatteryPowerColor;
        private string _lastBatteryPowerFormatted;

        // ── Gauge color getters (computed on demand; raised only when changed) ──

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

            // displayStep: 0.1 for watt gauges (show 1 decimal below 10), 1.0 for integer displays
            _packagePowerAnimated = new AnimatedDouble(0, 0.5, 0.1);
            _packagePowerAnimated.PropertyChanged += OnAnimatedValueChanged;

            _cpuTemperatureAnimated = new AnimatedDouble(0, 0.5, 1.0);
            _cpuTemperatureAnimated.PropertyChanged += OnAnimatedValueChanged;

            _cpuUsageAnimated = new AnimatedDouble(0, 0.5, 1.0);
            _cpuUsageAnimated.PropertyChanged += OnAnimatedValueChanged;

            _gpuUsageAnimated = new AnimatedDouble(0, 0.5, 1.0);
            _gpuUsageAnimated.PropertyChanged += OnAnimatedValueChanged;

            _fanSpeedAnimated = new AnimatedDouble(0, 0.5, 1.0);
            _fanSpeedAnimated.PropertyChanged += OnAnimatedValueChanged;

            _batteryPowerAnimated = new AnimatedDouble(0, 0.5, 0.1);
            _batteryPowerAnimated.PropertyChanged += OnAnimatedValueChanged;

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        // ── Core: re-raise only the derived properties that actually changed ──

        private void OnAnimatedValueChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == _packagePowerAnimated)
            {
                RaiseStringIfChanged(nameof(PackagePowerArc),
                    CalculateArcPath(PackagePowerPercentage), ref _lastPackagePowerArc);
                RaiseColorIfChanged(nameof(PackagePowerColor),
                    GetGradientColor(PackagePowerPercentage, 80.0, Colors.White, Colors.DarkOrange),
                    ref _lastPackagePowerColor);
                RaiseStringIfChanged(nameof(PackagePowerFormatted),
                    PackagePowerFormatted, ref _lastPackagePowerFormatted);
            }
            else if (sender == _cpuTemperatureAnimated)
            {
                RaiseStringIfChanged(nameof(CpuTemperatureArc),
                    CalculateArcPath(CpuTemperaturePercentage), ref _lastCpuTemperatureArc);
                RaiseColorIfChanged(nameof(CpuTemperatureColor),
                    GetGradientColor(CpuTemperaturePercentage, 100.0, Colors.White, Colors.Red),
                    ref _lastCpuTemperatureColor);
                RaiseStringIfChanged(nameof(CpuTemperatureFormatted),
                    CpuTemperatureFormatted, ref _lastCpuTemperatureFormatted);
            }
            else if (sender == _cpuUsageAnimated)
            {
                RaiseStringIfChanged(nameof(CpuUsageArc),
                    CalculateArcPath(CpuUsage), ref _lastCpuUsageArc);
                RaiseColorIfChanged(nameof(CpuUsageColor),
                    GetGradientColor(CpuUsage, 80.0, Colors.White, Colors.RoyalBlue),
                    ref _lastCpuUsageColor);
                RaiseStringIfChanged(nameof(CpuUsageFormatted),
                    CpuUsageFormatted, ref _lastCpuUsageFormatted);
            }
            else if (sender == _gpuUsageAnimated)
            {
                RaiseStringIfChanged(nameof(GpuUsageArc),
                    CalculateArcPath(GpuUsage), ref _lastGpuUsageArc);
                RaiseColorIfChanged(nameof(GpuUsageColor),
                    GetGradientColor(GpuUsage, 70.0, Colors.White, Colors.DarkOrchid),
                    ref _lastGpuUsageColor);
                RaiseStringIfChanged(nameof(GpuUsageFormatted),
                    GpuUsageFormatted, ref _lastGpuUsageFormatted);
            }
            else if (sender == _fanSpeedAnimated)
            {
                RaiseStringIfChanged(nameof(FanSpeedArc),
                    CalculateArcPath(FanSpeedPercentage), ref _lastFanSpeedArc);
                RaiseColorIfChanged(nameof(FanSpeedColor),
                    GetGradientColor(FanSpeedPercentage, 100.0, Colors.White, Colors.DeepSkyBlue),
                    ref _lastFanSpeedColor);
                RaiseStringIfChanged(nameof(FanSpeedFormatted),
                    FanSpeedFormatted, ref _lastFanSpeedFormatted);
            }
            else if (sender == _batteryPowerAnimated)
            {
                RaiseStringIfChanged(nameof(BatteryPowerArc),
                    CalculateArcPath(BatteryPowerPercentage), ref _lastBatteryPowerArc);
                RaiseColorIfChanged(nameof(BatteryPowerColor),
                    GetGradientColor(BatteryPowerPercentage, 80.0, Colors.White, Colors.Green),
                    ref _lastBatteryPowerColor);
                RaiseStringIfChanged(nameof(BatteryPowerFormatted),
                    BatteryPowerFormatted, ref _lastBatteryPowerFormatted);
            }
        }

        // ── Helpers: raise PropertyChanged only when value actually changed ──

        private void RaiseStringIfChanged(string propertyName, string newValue, ref string cache)
        {
            if (newValue != cache)
            {
                cache = newValue;
                OnPropertyChanged(propertyName);
            }
        }

        private void RaiseColorIfChanged(string propertyName, Color newValue, ref Color cache)
        {
            if (newValue != cache)
            {
                cache = newValue;
                OnPropertyChanged(propertyName);
            }
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

        // ── Package Power (W) ──

        public double PackagePower
        {
            get => _packagePowerAnimated.Value;
            set => _packagePowerAnimated.Target = value;
        }

        public string PackagePowerFormatted => PackagePower > 0 && Math.Floor(PackagePower) < 10
            ? PackagePower.ToString("F1", CultureInfo.InvariantCulture)
            : PackagePower.ToString("F0", CultureInfo.InvariantCulture);

        public double PackagePowerMax
        {
            get => _packagePowerMax;
            set
            {
                if (Math.Abs(_packagePowerMax - value) > 0.01)
                {
                    _packagePowerMax = value;
                    OnPropertyChanged();
                    // Sync caches so the next animation tick doesn't double-raise
                    _lastPackagePowerArc = CalculateArcPath(PackagePowerPercentage);
                    _lastPackagePowerColor = GetGradientColor(PackagePowerPercentage, 80.0, Colors.White, Colors.DarkOrange);
                    OnPropertyChanged(nameof(PackagePowerPercentage));
                    OnPropertyChanged(nameof(PackagePowerArc));
                    OnPropertyChanged(nameof(PackagePowerColor));
                }
            }
        }

        public double PackagePowerPercentage => (PackagePower / PackagePowerMax) * 100;
        public string PackagePowerArc => CalculateArcPath(PackagePowerPercentage);

        // ── CPU Temperature (°C) ──

        public double CpuTemperature
        {
            get => _cpuTemperatureAnimated.Value;
            set => _cpuTemperatureAnimated.Target = value;
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
                    _lastCpuTemperatureArc = CalculateArcPath(CpuTemperaturePercentage);
                    _lastCpuTemperatureColor = GetGradientColor(CpuTemperaturePercentage, 100.0, Colors.White, Colors.Red);
                    OnPropertyChanged(nameof(CpuTemperaturePercentage));
                    OnPropertyChanged(nameof(CpuTemperatureArc));
                    OnPropertyChanged(nameof(CpuTemperatureColor));
                }
            }
        }

        public double CpuTemperaturePercentage => (CpuTemperature / CpuTemperatureMax) * 100;
        public string CpuTemperatureFormatted => CpuTemperature.ToString("F0", CultureInfo.InvariantCulture);
        public string CpuTemperatureArc => CalculateArcPath(CpuTemperaturePercentage);

        // ── CPU Usage (%) ──

        public double CpuUsage
        {
            get => _cpuUsageAnimated.Value;
            set => _cpuUsageAnimated.Target = value;
        }

        public string CpuUsageFormatted => CpuUsage.ToString("F0", CultureInfo.InvariantCulture);
        public string CpuUsageArc => CalculateArcPath(CpuUsage);

        // ── GPU Usage (%) ──

        public double GpuUsage
        {
            get => _gpuUsageAnimated.Value;
            set => _gpuUsageAnimated.Target = value;
        }

        public string GpuUsageFormatted => GpuUsage.ToString("F0", CultureInfo.InvariantCulture);
        public string GpuUsageArc => CalculateArcPath(GpuUsage);

        // ── Fan Speed (RPM) ──

        public double FanSpeedDivided
        {
            get => _fanSpeedAnimated.Value / 10.0;
            set => _fanSpeedAnimated.Target = value * 10.0;
        }

        public double FanSpeed
        {
            get => _fanSpeedAnimated.Value;
            set => _fanSpeedAnimated.Target = value;
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
                    _lastFanSpeedArc = CalculateArcPath(FanSpeedPercentage);
                    _lastFanSpeedColor = GetGradientColor(FanSpeedPercentage, 100.0, Colors.White, Colors.DeepSkyBlue);
                    OnPropertyChanged(nameof(FanSpeedPercentage));
                    OnPropertyChanged(nameof(FanSpeedArc));
                    OnPropertyChanged(nameof(FanSpeedColor));
                }
            }
        }

        public double FanSpeedPercentage => (FanSpeed / FanSpeedMax) * 100;
        public string FanSpeedFormatted => FanSpeed.ToString("F0", CultureInfo.InvariantCulture);
        public string FanSpeedArc => CalculateArcPath(FanSpeedPercentage);

        // ── Battery Power (W) ──

        public double BatteryPower
        {
            get => _batteryPowerAnimated.Value;
            set => _batteryPowerAnimated.Target = value;
        }

        public string BatteryPowerFormatted => BatteryPower > 0 && Math.Floor(BatteryPower) < 10
            ? BatteryPower.ToString("F1", CultureInfo.InvariantCulture)
            : BatteryPower.ToString("F0", CultureInfo.InvariantCulture);

        public double BatteryPowerMax
        {
            get => _batteryPowerMax;
            set
            {
                if (Math.Abs(_batteryPowerMax - value) > 0.01)
                {
                    _batteryPowerMax = value;
                    OnPropertyChanged();
                    _lastBatteryPowerArc = CalculateArcPath(BatteryPowerPercentage);
                    _lastBatteryPowerColor = GetGradientColor(BatteryPowerPercentage, 80.0, Colors.White, Colors.Green);
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

            // For an arc under 180 degrees, the large-arc-flag should always be 0.
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
