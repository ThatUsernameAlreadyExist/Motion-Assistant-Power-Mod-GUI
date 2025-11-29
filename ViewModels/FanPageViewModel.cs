using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PmGui.Managers;
using PmGui.Resources.Localization;
using PmGui.ViewModels;

namespace PmGui.ViewModels.Pages
{
    public class FanPageViewModel : INotifyPropertyChanged
    {
        private LocalizationManager _localization = LocalizationManager.Instance;
        
        private bool _enableFanSpeedControlEnabled = false;
        private ObservableCollection<ComboBoxItemModel> _fanSpeedPresetItems;
        private ComboBoxItemModel _fanSpeedPresetSelectedItem;
        private ObservableCollection<ComboBoxItemModel> _fanSpeedControlTypeItems;
        private ComboBoxItemModel _fanSpeedControlTypeSelectedItem;
        private bool _isFixedSpeedMode = true;
        private bool _isSpeedCurveMode = false;
        private double _fanSpeedValue = 50;
        private double _fanSpeedMinValue = 10;
        private double _fanSpeedMaxValue = 100;
        private double _temperature45Speed = 30;
        private double _temperature60Speed = 50;
        private double _temperature70Speed = 70;
        private double _temperature80Speed = 90;
        private double _delayTimeoutValue = 5;

        // IsReadOnly fields
        private bool _isReadOnlyEnableFanSpeedControlEnabled = false;
        private bool _isReadOnlyFanSpeedPresetSelectedItem = false;
        private bool _isReadOnlyFanSpeedControlTypeSelectedItem = false;
        private bool _isReadOnlyIsFixedSpeedMode = false;
        private bool _isReadOnlyIsSpeedCurveMode = false;
        private bool _isReadOnlyFanSpeedValue = false;
        private bool _isReadOnlyTemperature45Speed = false;
        private bool _isReadOnlyTemperature60Speed = false;
        private bool _isReadOnlyTemperature70Speed = false;
        private bool _isReadOnlyTemperature80Speed = false;
        private bool _isReadOnlyDelayTimeoutValue = false;

        public FanPageViewModel()
        {
            _localization.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Localization));
                RefreshTranslations();
            };
            
            InitializeFanSpeedPresetItems();
            InitializeFanSpeedControlTypeItems();

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public LocalizationManager Localization => _localization;

        public void RefreshTranslations()
        {
            // Refresh combo box items with new translations
            var selectedPreset = FanSpeedPresetSelectedItem;
            var selectedControlType = FanSpeedControlTypeSelectedItem;
            
            InitializeFanSpeedPresetItems();
            InitializeFanSpeedControlTypeItems();
            
            FanSpeedPresetSelectedItem = selectedPreset;
            FanSpeedControlTypeSelectedItem = selectedControlType;
        }

        #region Properties

        public bool IsReadOnlyEnableFanSpeedControlEnabled
        {
            get => _isReadOnlyEnableFanSpeedControlEnabled;
            set => SetProperty(ref _isReadOnlyEnableFanSpeedControlEnabled, value);
        }

        public bool IsReadOnlyFanSpeedPresetSelectedItem
        {
            get => _isReadOnlyFanSpeedPresetSelectedItem;
            set => SetProperty(ref _isReadOnlyFanSpeedPresetSelectedItem, value);
        }

        public bool IsReadOnlyFanSpeedControlTypeSelectedItem
        {
            get => _isReadOnlyFanSpeedControlTypeSelectedItem;
            set => SetProperty(ref _isReadOnlyFanSpeedControlTypeSelectedItem, value);
        }

        public bool IsReadOnlyIsFixedSpeedMode
        {
            get => _isReadOnlyIsFixedSpeedMode;
            set => SetProperty(ref _isReadOnlyIsFixedSpeedMode, value);
        }

        public bool IsReadOnlyIsSpeedCurveMode
        {
            get => _isReadOnlyIsSpeedCurveMode;
            set => SetProperty(ref _isReadOnlyIsSpeedCurveMode, value);
        }

        public bool IsReadOnlyFanSpeedValue
        {
            get => _isReadOnlyFanSpeedValue;
            set => SetProperty(ref _isReadOnlyFanSpeedValue, value);
        }

        public bool IsReadOnlyTemperature45Speed
        {
            get => _isReadOnlyTemperature45Speed;
            set => SetProperty(ref _isReadOnlyTemperature45Speed, value);
        }

        public bool IsReadOnlyTemperature60Speed
        {
            get => _isReadOnlyTemperature60Speed;
            set => SetProperty(ref _isReadOnlyTemperature60Speed, value);
        }

        public bool IsReadOnlyTemperature70Speed
        {
            get => _isReadOnlyTemperature70Speed;
            set => SetProperty(ref _isReadOnlyTemperature70Speed, value);
        }

        public bool IsReadOnlyTemperature80Speed
        {
            get => _isReadOnlyTemperature80Speed;
            set => SetProperty(ref _isReadOnlyTemperature80Speed, value);
        }

        public bool IsReadOnlyDelayTimeoutValue
        {
            get => _isReadOnlyDelayTimeoutValue;
            set => SetProperty(ref _isReadOnlyDelayTimeoutValue, value);
        }

        public bool EnableFanSpeedControlEnabled
        {
            get => _enableFanSpeedControlEnabled;
            set
            {
                if (_isReadOnlyEnableFanSpeedControlEnabled) return;
                if (SetProperty(ref _enableFanSpeedControlEnabled, value))
                {
                    var oldValue = !value;
                    GlobalAppManager.Instance.SendCmdEnableFanSpeedControlEnabled(value);
                }
            }
        }

        public ObservableCollection<ComboBoxItemModel> FanSpeedPresetItems
        {
            get => _fanSpeedPresetItems;
            private set => SetProperty(ref _fanSpeedPresetItems, value);
        }

        public ComboBoxItemModel FanSpeedPresetSelectedItem
        {
            get => _fanSpeedPresetSelectedItem;
            set
            {
                if (_isReadOnlyFanSpeedPresetSelectedItem) return;
                if (SetProperty(ref _fanSpeedPresetSelectedItem, value))
                {
                    var oldValue = _fanSpeedPresetSelectedItem;
                    GlobalAppManager.Instance.SendCmdFanSpeedPresetSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ObservableCollection<ComboBoxItemModel> FanSpeedControlTypeItems
        {
            get => _fanSpeedControlTypeItems;
            private set => SetProperty(ref _fanSpeedControlTypeItems, value);
        }

        public ComboBoxItemModel FanSpeedControlTypeSelectedItem
        {
            get => _fanSpeedControlTypeSelectedItem;
            set
            {
                if (_isReadOnlyFanSpeedControlTypeSelectedItem) return;
                if (SetProperty(ref _fanSpeedControlTypeSelectedItem, value))
                {
                    var oldValue = _fanSpeedControlTypeSelectedItem;
                    UpdateControlTypeMode();
                    GlobalAppManager.Instance.SendCmdFanSpeedControlTypeSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public bool IsFixedSpeedMode
        {
            get => _isFixedSpeedMode;
            set
            {
                if (_isReadOnlyIsFixedSpeedMode) return;
                if (SetProperty(ref _isFixedSpeedMode, value))
                {
                    var oldValue = !value;
                    GlobalAppManager.Instance.SendCmdIsFixedSpeedMode(value);
                }
            }
        }

        public bool IsSpeedCurveMode
        {
            get => _isSpeedCurveMode;
            set
            {
                if (_isReadOnlyIsSpeedCurveMode) return;
                if (SetProperty(ref _isSpeedCurveMode, value))
                {
                    var oldValue = !value;
                    GlobalAppManager.Instance.SendCmdIsSpeedCurveMode(value);
                }
            }
        }

        public double FanSpeedValue
        {
            get => _fanSpeedValue;
            set
            {
                if (_isReadOnlyFanSpeedValue) return;
                if (SetProperty(ref _fanSpeedValue, value))
                {
                    var oldValue = _fanSpeedValue;
                    // Cascade logic preserved - no GlobalAppManager call
                }
            }
        }

        public double FanSpeedMinValue
        {
            get => _fanSpeedMinValue;
            set => SetProperty(ref _fanSpeedMinValue, value);
        }

        public double FanSpeedMaxValue
        {
            get => _fanSpeedMaxValue;
            set => SetProperty(ref _fanSpeedMaxValue, value);
        }

        public double Temperature45Speed
        {
            get => _temperature45Speed;
            set
            {
                if (_isReadOnlyTemperature45Speed) return;
                if (SetProperty(ref _temperature45Speed, value))
                {
                    var oldValue = _temperature45Speed;
                    // Cascade upward: ensure next slider is not less than this one
                    if (_temperature60Speed < value)
                    {
                        Temperature60Speed = value;
                    }
                    // Cascade logic preserved - no GlobalAppManager call
                }
            }
        }

        public double Temperature60Speed
        {
            get => _temperature60Speed;
            set
            {
                if (_isReadOnlyTemperature60Speed) return;
                if (SetProperty(ref _temperature60Speed, value))
                {
                    var oldValue = _temperature60Speed;
                    // Cascade downward: if decreased below previous slider
                    if (value < _temperature45Speed)
                    {
                        _temperature45Speed = value;
                        OnPropertyChanged(nameof(Temperature45Speed));
                    }
                    // Cascade upward: ensure next slider is not less than this one
                    else if (_temperature70Speed < value)
                    {
                        Temperature70Speed = value;
                    }
                    // Cascade logic preserved - no GlobalAppManager call
                }
            }
        }

        public double Temperature70Speed
        {
            get => _temperature70Speed;
            set
            {
                if (_isReadOnlyTemperature70Speed) return;
                if (SetProperty(ref _temperature70Speed, value))
                {
                    var oldValue = _temperature70Speed;
                    // Cascade downward: if decreased below previous slider
                    if (value < _temperature60Speed)
                    {
                        Temperature60Speed = value;
                    }
                    // Cascade upward: ensure next slider is not less than this one
                    else if (_temperature80Speed < value)
                    {
                        Temperature80Speed = value;
                    }
                    // Cascade logic preserved - no GlobalAppManager call
                }
            }
        }

        public double Temperature80Speed
        {
            get => _temperature80Speed;
            set
            {
                if (_isReadOnlyTemperature80Speed) return;
                if (SetProperty(ref _temperature80Speed, value))
                {
                    var oldValue = _temperature80Speed;
                    // Cascade downward: if decreased below previous slider
                    if (value < _temperature70Speed)
                    {
                        Temperature70Speed = value;
                    }
                    // Cascade logic preserved - no GlobalAppManager call
                }
            }
        }

        public double DelayTimeoutValue
        {
            get => _delayTimeoutValue;
            set
            {
                if (_isReadOnlyDelayTimeoutValue) return;
                if (SetProperty(ref _delayTimeoutValue, value))
                {
                    var oldValue = _delayTimeoutValue;
                    // Validation and property logic preserved - no GlobalAppManager call
                }
            }
        }

        #endregion


        #region Methods

        private void InitializeFanSpeedPresetItems()
        {
            _fanSpeedPresetItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayName = _localization["Quiet"], Value = 0, Id = "0" },
                new ComboBoxItemModel { DisplayName = _localization["Balanced"], Value = 1, Id = "1" },
                new ComboBoxItemModel { DisplayName = _localization["Performance"], Value = 2, Id = "2" },
                new ComboBoxItemModel { DisplayName = _localization["Custom"], Value = 3, Id = "3" }
            };
            
            // Set default selection if not set
            if (_fanSpeedPresetSelectedItem == null || !_fanSpeedPresetItems.Contains(_fanSpeedPresetSelectedItem))
            {
                _fanSpeedPresetSelectedItem = _fanSpeedPresetItems[0];
            }
            
            OnPropertyChanged(nameof(FanSpeedPresetItems));
        }

        private void InitializeFanSpeedControlTypeItems()
        {
            _fanSpeedControlTypeItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayName = _localization["FixedSpeed"], Value = 0, Id = "fixed" },
                new ComboBoxItemModel { DisplayName = _localization["SpeedCurve"], Value = 1, Id = "curve" }
            };
            
            // Set default selection if not set
            if (_fanSpeedControlTypeSelectedItem == null || !_fanSpeedControlTypeItems.Contains(_fanSpeedControlTypeSelectedItem))
            {
                _fanSpeedControlTypeSelectedItem = _fanSpeedControlTypeItems[0];
            }
            
            OnPropertyChanged(nameof(FanSpeedControlTypeItems));
        }

        private void UpdateControlTypeMode()
        {
            if (_fanSpeedControlTypeSelectedItem?.Id == "fixed")
            {
                IsFixedSpeedMode = true;
                IsSpeedCurveMode = false;
            }
            else if (_fanSpeedControlTypeSelectedItem?.Id == "curve")
            {
                IsFixedSpeedMode = false;
                IsSpeedCurveMode = true;
            }
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