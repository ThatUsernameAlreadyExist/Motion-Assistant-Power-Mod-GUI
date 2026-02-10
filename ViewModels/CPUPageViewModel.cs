using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PmGui.Managers;
using PmGui.Resources.Localization;

namespace PmGui.ViewModels.Pages
{
    public class CPUPageViewModel : INotifyPropertyChanged
    {
        private LocalizationManager _localization = LocalizationManager.Instance;
        
        // CPU Model
        private string _cpuModel = "CPU";
        
        // TDP values
        private double _actualTdpValue = 0;
        private double _powerLineTdpValue = 0;
        private double _batteryTdpValue = 0;
        private double _tdpMinValue = 4;
        private double _tdpMaxValue = 30;
        
        // TDP Preset values
        private List<int> _tdpPresetValues = new List<int> { 5, 8, 10, 12, 15, 18 };
        
        // Active profile
        private bool _isPowerLineActive = false;
        private bool _isBatteryActive = false;

        // Toggle switches
        private bool _cpuBoostEnabled = true;
        private bool _autoOptimizeCpuFrequencyEnabled = false;
        private bool _uniteBatteryAndPowerlineCPUPresetsEnabled = false;
        private bool _uniteBatteryAndPowerlineFPSLimitEnabled = false;
        private bool _loadPresetAtStartEnabled = false;

        // Command names
        private string _setPowerLineTdpPresetCommandName = "SetPowerLineTdpPresetCommand";
        private string _setBatteryTdpPresetCommandName = "SetBatteryTdpPresetCommand";

        // Read-only state properties for controls
        private bool _isReadOnlyPowerLineTdpValue = false;
        private bool _isReadOnlyBatteryTdpValue = false;
        private bool _isReadOnlyIsPowerLineActive = false;
        private bool _isReadOnlyCPUBoostEnabled = false;
        private bool _isReadOnlyAutoOptimizeCpuFrequencyEnabled = false;
        private bool _isReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled = false;
        private bool _isReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled = false;
        private bool _isReadOnlyLoadPresetAtStartEnabled = false;
        private bool _isReadOnlyPowerLineFrequencySelectedItem = false;
        private bool _isReadOnlyPowerLineFpsSelectedItem = false;
        private bool _isReadOnlyPowerLineCpuCoresSelectedItem = false;
        private bool _isReadOnlyBatteryFrequencySelectedItem = false;
        private bool _isReadOnlyBatteryFpsSelectedItem = false;
        private bool _isReadOnlyBatteryCpuCoresSelectedItem = false;
        private bool _isReadOnlySetPowerLineTdpPresetCommand = false;
        private bool _isReadOnlySetBatteryTdpPresetCommand = false;

        public CPUPageViewModel()
        {
            // Subscribe to language changes to refresh ComboBox items
            _localization.PropertyChanged += (s, e) => RefreshTranslations();
            
            InitializeComboBoxes();
            SetPowerLineTdpPresetCommand = new RelayCommand(SetPowerLineTdpPreset);
            SetBatteryTdpPresetCommand = new RelayCommand(SetBatteryTdpPreset);

            SendPowerLineTdpCommand = new RelayCommand(param =>
            {
                if (param is int value)
                {
                    System.Diagnostics.Debug.WriteLine($"SendPowerLineTdp: {value}");
                    GlobalAppManager.Instance.SendCmdPowerLineTdpValue(value);
                }
            });

            SendBatteryTdpCommand = new RelayCommand(param =>
            {
                if (param is int value)
                {
                    System.Diagnostics.Debug.WriteLine($"SendBatteryTdp: {value}");
                    GlobalAppManager.Instance.SendCmdBatteryTdpValue(value);
                }
            });

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public LocalizationManager Localization => _localization;

        public void RefreshTranslations()
        {
            // Refresh all ComboBox items with new translations
            var powerLineFreqValue = PowerLineFrequencySelectedItem?.Value ?? 0;
            var batteryFreqValue = BatteryFrequencySelectedItem?.Value ?? 0;
            var powerLineFpsValue = PowerLineFpsSelectedItem?.Value ?? 0;
            var batteryFpsValue = BatteryFpsSelectedItem?.Value ?? 0;
            var powerLineCoresValue = PowerLineCpuCoresSelectedItem?.Value ?? 0;
            var batteryCoresValue = BatteryCpuCoresSelectedItem?.Value ?? 0;
            
            // Reload all items
            InitializeComboBoxes();
            
            // Notify UI that collections have changed
            OnPropertyChanged(nameof(PowerLineFrequencyItems));
            OnPropertyChanged(nameof(BatteryFrequencyItems));
            OnPropertyChanged(nameof(PowerLineFpsItems));
            OnPropertyChanged(nameof(BatteryFpsItems));
            OnPropertyChanged(nameof(PowerLineCpuCoresItems));
            OnPropertyChanged(nameof(BatteryCpuCoresItems));
            
            // Restore selections
            if (PowerLineFrequencyItems != null)
                PowerLineFrequencySelectedItem = PowerLineFrequencyItems.FirstOrDefault(x => x.Value == powerLineFreqValue);
            if (BatteryFrequencyItems != null)
                BatteryFrequencySelectedItem = BatteryFrequencyItems.FirstOrDefault(x => x.Value == batteryFreqValue);
            if (PowerLineFpsItems != null)
                PowerLineFpsSelectedItem = PowerLineFpsItems.FirstOrDefault(x => x.Value == powerLineFpsValue);
            if (BatteryFpsItems != null)
                BatteryFpsSelectedItem = BatteryFpsItems.FirstOrDefault(x => x.Value == batteryFpsValue);
            if (PowerLineCpuCoresItems != null)
                PowerLineCpuCoresSelectedItem = PowerLineCpuCoresItems.FirstOrDefault(x => x.Value == powerLineCoresValue);
            if (BatteryCpuCoresItems != null)
                BatteryCpuCoresSelectedItem = BatteryCpuCoresItems.FirstOrDefault(x => x.Value == batteryCoresValue);
        }


        #region Properties

        public string SetPowerLineTdpPresetCommandName
        {
            get => _setPowerLineTdpPresetCommandName;
            set => SetProperty(ref _setPowerLineTdpPresetCommandName, value);
        }

        public string SetBatteryTdpPresetCommandName
        {
            get => _setBatteryTdpPresetCommandName;
            set => SetProperty(ref _setBatteryTdpPresetCommandName, value);
        }

        public string CPUModel
        {
            get => _cpuModel;
            set
            {
                var cpuText = value;
                int limit = value.IndexOf(" w/");
                if (limit > 0)
                {
                    cpuText = value.Substring(0, limit);
                }

                if (cpuText.Length > 24)
                {
                    cpuText = cpuText.Substring(0, 24) + "...";
                }
                    
                SetProperty(ref _cpuModel, cpuText);
            }
        }

        public double PowerLineTdpValue
        {
            get => _powerLineTdpValue;
            set
            {
                if (IsReadOnlyPowerLineTdpValue)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(PowerLineTdpValue));
                    return;
                }

                SetProperty(ref _powerLineTdpValue, value);
            }
        }

        public double BatteryTdpValue
        {
            get => _batteryTdpValue;
            set
            {
                if (IsReadOnlyBatteryTdpValue)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(BatteryTdpValue));
                    return;
                }

                SetProperty(ref _batteryTdpValue, value);
            }
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        public double ActualTdpValue
        {
            get => _actualTdpValue;
            set
            {
                if (SetProperty(ref _actualTdpValue, value))
                {
                    if (_isPowerLineActive)
                    {
                        PowerLineTdpValue = Clamp(value, TdpMinValue, TdpMaxValue);
                    }
                    else
                    {
                        BatteryTdpValue = Clamp(value, TdpMinValue, TdpMaxValue);
                    }
                }
            }
        }
        
        public double TdpMinValue
        {
            get => _tdpMinValue;
            set => SetProperty(ref _tdpMinValue, value);
        }

        public double TdpMaxValue
        {
            get => _tdpMaxValue;
            set => SetProperty(ref _tdpMaxValue, value);
        }

        public List<int> TdpPresetValues
        {
            get => _tdpPresetValues;
            set
            {
                if (SetProperty(ref _tdpPresetValues, value))
                {
                    OnPropertyChanged(nameof(TdpPreset1));
                    OnPropertyChanged(nameof(TdpPreset2));
                    OnPropertyChanged(nameof(TdpPreset3));
                    OnPropertyChanged(nameof(TdpPreset4));
                    OnPropertyChanged(nameof(TdpPreset5));
                    OnPropertyChanged(nameof(TdpPreset6));
                }
            }
        }

        public int TdpPreset1 => _tdpPresetValues.Count > 0 ? _tdpPresetValues[0] : 5;
        public int TdpPreset2 => _tdpPresetValues.Count > 1 ? _tdpPresetValues[1] : 8;
        public int TdpPreset3 => _tdpPresetValues.Count > 2 ? _tdpPresetValues[2] : 10;
        public int TdpPreset4 => _tdpPresetValues.Count > 3 ? _tdpPresetValues[3] : 12;
        public int TdpPreset5 => _tdpPresetValues.Count > 4 ? _tdpPresetValues[4] : 15;
        public int TdpPreset6 => _tdpPresetValues.Count > 5 ? _tdpPresetValues[5] : 18;

        public bool IsPowerLineActive
        {
            get => _isPowerLineActive;
            set
            {
                if (SetProperty(ref _isPowerLineActive, value))
                {
                    OnPropertyChanged(nameof(IsPowerLineExpanded));
                }
            }
        }

        public bool IsBatteryActive
        {
            get => _isBatteryActive;
            set
            {
                if (SetProperty(ref _isBatteryActive, value))
                {
                    OnPropertyChanged(nameof(IsBatteryExpanded));
                }
            }
        }

        public bool IsPowerLineExpanded => _isPowerLineActive || _uniteBatteryAndPowerlineCPUPresetsEnabled;
        public bool IsBatteryExpanded => _isBatteryActive && !_uniteBatteryAndPowerlineCPUPresetsEnabled;

        public bool CPUBoostEnabled
        {
            get => _cpuBoostEnabled;
            set
            {
                if (IsReadOnlyCPUBoostEnabled)
                {
                    OnPropertyChanged(nameof(CPUBoostEnabled));
                    return;
                }

                if (SetProperty(ref _cpuBoostEnabled, value))
                {
                    GlobalAppManager.Instance.SendCmdCPUBoostEnabled(value);
                }
            }
        }

        public bool AutoOptimizeCpuFrequencyEnabled
        {
            get => _autoOptimizeCpuFrequencyEnabled;
            set
            {
                if (IsReadOnlyAutoOptimizeCpuFrequencyEnabled)
                {
                    OnPropertyChanged(nameof(AutoOptimizeCpuFrequencyEnabled));
                    return;
                }

                if (SetProperty(ref _autoOptimizeCpuFrequencyEnabled, value))
                {
                    GlobalAppManager.Instance.SendCmdAutoOptimizeCpuFrequencyEnabled(value);
                }
            }
        }

        public bool UniteBatteryAndPowerlineCPUPresetsEnabled
        {
            get => _uniteBatteryAndPowerlineCPUPresetsEnabled;
            set
            {
                if (IsReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled)
                {
                    OnPropertyChanged(nameof(UniteBatteryAndPowerlineCPUPresetsEnabled));
                    return;
                }

                if (SetProperty(ref _uniteBatteryAndPowerlineCPUPresetsEnabled, value))
                {
                    GlobalAppManager.Instance.SendCmdUniteBatteryAndPowerlineCPUPresetsEnabled(value);
                    OnPropertyChanged(nameof(IsBatteryExpanded));
                    OnPropertyChanged(nameof(IsPowerLineExpanded));
                }
            }
        }

        public bool UniteBatteryAndPowerlineFPSLimitEnabled
        {
            get => _uniteBatteryAndPowerlineFPSLimitEnabled;
            set
            {
                if (IsReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled)
                {
                    OnPropertyChanged(nameof(UniteBatteryAndPowerlineFPSLimitEnabled));
                    return;
                }

                if (SetProperty(ref _uniteBatteryAndPowerlineFPSLimitEnabled, value))
                {
                    GlobalAppManager.Instance.SendCmdUniteBatteryAndPowerlineFPSLimitEnabled(value);
                }
            }
        }

        public bool LoadPresetAtStartEnabled
        {
            get => _loadPresetAtStartEnabled;
            set
            {
                if (IsReadOnlyLoadPresetAtStartEnabled)
                {
                    OnPropertyChanged(nameof(LoadPresetAtStartEnabled));
                    return;
                }

                if (SetProperty(ref _loadPresetAtStartEnabled, value))
                {
                    GlobalAppManager.Instance.SendCmdLoadPresetAtStartEnabled(value);
                }
            }
        }

        #endregion

        #region ComboBox Items

        public ObservableCollection<ComboBoxItemModel> PowerLineFrequencyItems { get; private set; }
        public ObservableCollection<ComboBoxItemModel> PowerLineFpsItems { get; private set; }
        public ObservableCollection<ComboBoxItemModel> PowerLineCpuCoresItems { get; private set; }

        public ObservableCollection<ComboBoxItemModel> BatteryFrequencyItems { get; private set; }
        public ObservableCollection<ComboBoxItemModel> BatteryFpsItems { get; private set; }
        public ObservableCollection<ComboBoxItemModel> BatteryCpuCoresItems { get; private set; }

        private ComboBoxItemModel _powerLineFrequencySelectedItem;
        private ComboBoxItemModel _powerLineFpsSelectedItem;
        private ComboBoxItemModel _powerLineCpuCoresSelectedItem;

        private ComboBoxItemModel _batteryFrequencySelectedItem;
        private ComboBoxItemModel _batteryFpsSelectedItem;
        private ComboBoxItemModel _batteryCpuCoresSelectedItem;

        public ComboBoxItemModel PowerLineFrequencySelectedItem
        {
            get => _powerLineFrequencySelectedItem;
            set
            {
                if (IsReadOnlyPowerLineFrequencySelectedItem)
                {
                    OnPropertyChanged(nameof(PowerLineFrequencySelectedItem));
                    return;
                }

                if (SetProperty(ref _powerLineFrequencySelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdPowerLineFrequencySelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ComboBoxItemModel PowerLineFpsSelectedItem
        {
            get => _powerLineFpsSelectedItem;
            set
            {
                if (IsReadOnlyPowerLineFpsSelectedItem)
                {
                    OnPropertyChanged(nameof(PowerLineFpsSelectedItem));
                    return;
                }

                if (SetProperty(ref _powerLineFpsSelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdPowerLineFpsSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ComboBoxItemModel PowerLineCpuCoresSelectedItem
        {
            get => _powerLineCpuCoresSelectedItem;
            set
            {
                if (IsReadOnlyPowerLineCpuCoresSelectedItem)
                {
                    OnPropertyChanged(nameof(PowerLineCpuCoresSelectedItem));
                    return;
                }

                if (SetProperty(ref _powerLineCpuCoresSelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdPowerLineCpuCoresSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ComboBoxItemModel BatteryFrequencySelectedItem
        {
            get => _batteryFrequencySelectedItem;
            set
            {
                if (IsReadOnlyBatteryFrequencySelectedItem)
                {
                    OnPropertyChanged(nameof(BatteryFrequencySelectedItem));
                    return;
                }

                if (SetProperty(ref _batteryFrequencySelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdBatteryFrequencySelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ComboBoxItemModel BatteryFpsSelectedItem
        {
            get => _batteryFpsSelectedItem;
            set
            {
                if (IsReadOnlyBatteryFpsSelectedItem)
                {
                    OnPropertyChanged(nameof(BatteryFpsSelectedItem));
                    return;
                }

                if (SetProperty(ref _batteryFpsSelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdBatteryFpsSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ComboBoxItemModel BatteryCpuCoresSelectedItem
        {
            get => _batteryCpuCoresSelectedItem;
            set
            {
                if (IsReadOnlyBatteryCpuCoresSelectedItem)
                {
                    OnPropertyChanged(nameof(BatteryCpuCoresSelectedItem));
                    return;
                }

                if (SetProperty(ref _batteryCpuCoresSelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdBatteryCpuCoresSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        #endregion

        #region Read-Only Control Properties

        public bool IsReadOnlyPowerLineTdpValue
        {
            get => _isReadOnlyPowerLineTdpValue;
            set => SetProperty(ref _isReadOnlyPowerLineTdpValue, value);
        }

        public bool IsReadOnlyBatteryTdpValue
        {
            get => _isReadOnlyBatteryTdpValue;
            set => SetProperty(ref _isReadOnlyBatteryTdpValue, value);
        }

        public bool IsReadOnlyIsPowerLineActive
        {
            get => _isReadOnlyIsPowerLineActive;
            set => SetProperty(ref _isReadOnlyIsPowerLineActive, value);
        }

        public bool IsReadOnlyCPUBoostEnabled
        {
            get => _isReadOnlyCPUBoostEnabled;
            set => SetProperty(ref _isReadOnlyCPUBoostEnabled, value);
        }

        public bool IsReadOnlyAutoOptimizeCpuFrequencyEnabled
        {
            get => _isReadOnlyAutoOptimizeCpuFrequencyEnabled;
            set => SetProperty(ref _isReadOnlyAutoOptimizeCpuFrequencyEnabled, value);
        }

        public bool IsReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled
        {
            get => _isReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled;
            set => SetProperty(ref _isReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled, value);
        }

        public bool IsReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled
        {
            get => _isReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled;
            set => SetProperty(ref _isReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled, value);
        }

        public bool IsReadOnlyLoadPresetAtStartEnabled
        {
            get => _isReadOnlyLoadPresetAtStartEnabled;
            set => SetProperty(ref _isReadOnlyLoadPresetAtStartEnabled, value);
        }

        public bool IsReadOnlyPowerLineFrequencySelectedItem
        {
            get => _isReadOnlyPowerLineFrequencySelectedItem;
            set => SetProperty(ref _isReadOnlyPowerLineFrequencySelectedItem, value);
        }

        public bool IsReadOnlyPowerLineFpsSelectedItem
        {
            get => _isReadOnlyPowerLineFpsSelectedItem;
            set => SetProperty(ref _isReadOnlyPowerLineFpsSelectedItem, value);
        }

        public bool IsReadOnlyPowerLineCpuCoresSelectedItem
        {
            get => _isReadOnlyPowerLineCpuCoresSelectedItem;
            set => SetProperty(ref _isReadOnlyPowerLineCpuCoresSelectedItem, value);
        }

        public bool IsReadOnlyBatteryFrequencySelectedItem
        {
            get => _isReadOnlyBatteryFrequencySelectedItem;
            set => SetProperty(ref _isReadOnlyBatteryFrequencySelectedItem, value);
        }

        public bool IsReadOnlyBatteryFpsSelectedItem
        {
            get => _isReadOnlyBatteryFpsSelectedItem;
            set => SetProperty(ref _isReadOnlyBatteryFpsSelectedItem, value);
        }

        public bool IsReadOnlyBatteryCpuCoresSelectedItem
        {
            get => _isReadOnlyBatteryCpuCoresSelectedItem;
            set => SetProperty(ref _isReadOnlyBatteryCpuCoresSelectedItem, value);
        }

        public bool IsReadOnlySetPowerLineTdpPresetCommand
        {
            get => _isReadOnlySetPowerLineTdpPresetCommand;
            set => SetProperty(ref _isReadOnlySetPowerLineTdpPresetCommand, value);
        }

        public bool IsReadOnlySetBatteryTdpPresetCommand
        {
            get => _isReadOnlySetBatteryTdpPresetCommand;
            set => SetProperty(ref _isReadOnlySetBatteryTdpPresetCommand, value);
        }

        #endregion

        #region Commands

        public ICommand SetPowerLineTdpPresetCommand { get; }
        public ICommand SetBatteryTdpPresetCommand { get; }

        public ICommand SendPowerLineTdpCommand { get; }
        public ICommand SendBatteryTdpCommand { get; }

        private void SetPowerLineTdpPreset(object parameter)
        {
            if (parameter == null) return;
            
            double value = 0;
            
            if (parameter is string str && double.TryParse(str, out double parsed))
            {
                value = parsed;
            }
            else if (parameter is int intVal)
            {
                value = intVal;
            }
            else if (parameter is double doubleVal)
            {
                value = doubleVal;
            }
            else
            {
                return;
            }
            
            PowerLineTdpValue = value;

            // No need to call: GlobalAppManager.Instance.SendCmdPowerLineTdpValue(PowerLineTdpValue);
            // Will be automatically fired by slider handler SliderKeyboardBehavior because we change 'PowerLineTdpValue'
        }

        private void SetBatteryTdpPreset(object parameter)
        {
            if (parameter == null) return;
            
            double value = 0;
            
            if (parameter is string str && double.TryParse(str, out double parsed))
            {
                value = parsed;
            }
            else if (parameter is int intVal)
            {
                value = intVal;
            }
            else if (parameter is double doubleVal)
            {
                value = doubleVal;
            }
            else
            {
                return;
            }
            
            BatteryTdpValue = value;

            // No need to call: GlobalAppManager.Instance.SendCmdBatteryTdpValue(BatteryTdpValue);
            // Will be automatically fired by slider handler SliderKeyboardBehavior because we change 'PowerLineTdpValue'
        }

        #endregion

        #region Methods

        private void InitializeComboBoxes()
        {
            // CPU Frequency items
            var frequencyItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayName = _localization["Max"], Value = 0, Id = "0" }
            };
            for (int i = 1000; i <= 5000; i += 250)
            {
                frequencyItems.Add(new ComboBoxItemModel { DisplayName = $"{i}", Value = i, Id = $"{i}" });
            }

            PowerLineFrequencyItems = new ObservableCollection<ComboBoxItemModel>(frequencyItems);
            BatteryFrequencyItems = new ObservableCollection<ComboBoxItemModel>(frequencyItems);
            PowerLineFrequencySelectedItem = PowerLineFrequencyItems[0];
            BatteryFrequencySelectedItem = BatteryFrequencyItems[0];

            // FPS items
            var fpsValues = new[] { 0, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120 };
            var fpsItems = new ObservableCollection<ComboBoxItemModel>();
            foreach (var fps in fpsValues)
            {
                fpsItems.Add(new ComboBoxItemModel
                {
                    DisplayName = fps == 0 ? _localization["None"] : $"{fps}",
                    Value = fps,
                    Id = fps == 0 ? "0" : $"{fps}"
                });
            }

            PowerLineFpsItems = new ObservableCollection<ComboBoxItemModel>(fpsItems);
            BatteryFpsItems = new ObservableCollection<ComboBoxItemModel>(fpsItems);
            PowerLineFpsSelectedItem = PowerLineFpsItems[0];
            BatteryFpsSelectedItem = BatteryFpsItems[0];

            // CPU Cores items
            var coresItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayName = _localization["All"], Value = 0, Id = "0" },
                new ComboBoxItemModel { DisplayName = _localization["PriorityBig"], Value = 3, Id = "1" },
                new ComboBoxItemModel { DisplayName = _localization["PrioritySmall"], Value = 4, Id = "2" },
                new ComboBoxItemModel { DisplayName = _localization["OnlyBig"], Value = 1, Id = "3" },
                new ComboBoxItemModel { DisplayName = _localization["OnlySmall"], Value = 2, Id = "4" },

            };

            PowerLineCpuCoresItems = new ObservableCollection<ComboBoxItemModel>(coresItems);
            BatteryCpuCoresItems = new ObservableCollection<ComboBoxItemModel>(coresItems);
            PowerLineCpuCoresSelectedItem = PowerLineCpuCoresItems[0];
            BatteryCpuCoresSelectedItem = BatteryCpuCoresItems[0];
        }

        public void UpdateFrequencyItems(int minFreq, int maxFreq, int step)
        {
            var frequencyItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayName = _localization["Max"], Value = 0 }
            };
            for (int i = minFreq; i <= maxFreq; i += step)
            {
                frequencyItems.Add(new ComboBoxItemModel { DisplayName = $"{i}", Value = i });
            }

            PowerLineFrequencyItems = new ObservableCollection<ComboBoxItemModel>(frequencyItems);
            BatteryFrequencyItems = new ObservableCollection<ComboBoxItemModel>(frequencyItems);
            OnPropertyChanged(nameof(PowerLineFrequencyItems));
            OnPropertyChanged(nameof(BatteryFrequencyItems));
        }

        public void UpdateFpsItems(int[] fpsValues)
        {
            var fpsItems = new ObservableCollection<ComboBoxItemModel>();
            foreach (var fps in fpsValues)
            {
                fpsItems.Add(new ComboBoxItemModel
                {
                    DisplayName = fps == 0 ? _localization["None"] : $"{fps}",
                    Value = fps
                });
            }

            PowerLineFpsItems = new ObservableCollection<ComboBoxItemModel>(fpsItems);
            BatteryFpsItems = new ObservableCollection<ComboBoxItemModel>(fpsItems);
            OnPropertyChanged(nameof(PowerLineFpsItems));
            OnPropertyChanged(nameof(BatteryFpsItems));
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }


}