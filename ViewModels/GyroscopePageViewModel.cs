using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PmGui.Managers;
using PmGui.Resources.Localization;

namespace PmGui.ViewModels.Pages
{
    public class GyroscopePageViewModel : INotifyPropertyChanged
    {
        private LocalizationManager _localization = LocalizationManager.Instance;
        
        private bool _enableGyroscope = false;
        private bool _autoEnableGyroscopeOnStart = false;
        private bool _highPrecisionGyroscope = false;
        private bool _disableBoschAccelerometer = false;

        private bool _isReadOnlyEnableGyroscope = false;
        private bool _isReadOnlyAutoEnableGyroscopeOnStart = false;
        private bool _isReadOnlyHighPrecisionGyroscope = false;
        private bool _isReadOnlyDisableBoschAccelerometer = false;

        private ObservableCollection<ComboBoxItemModel> _gyroscopeActivationButtonItems;
        private ComboBoxItemModel _gyroscopeActivationButtonSelectedItem;
        private bool _isReadOnlyGyroscopeActivationButton = false;

        public GyroscopePageViewModel()
        {
            // Subscribe to language changes to refresh ComboBox items
            _localization.PropertyChanged += (s, e) => RefreshTranslations();
            
            InitializeGyroscopeActivationButtonItems();

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public void RefreshTranslations()
        {
            // Refresh combo box items with new translations
            var selectedItemId = GyroscopeActivationButtonSelectedItem?.Id ?? string.Empty;
            InitializeGyroscopeActivationButtonItems();

            // Restore selection by ID if it exists
            if (!string.IsNullOrEmpty(selectedItemId) && GyroscopeActivationButtonItems != null)
            {
                GyroscopeActivationButtonSelectedItem = GyroscopeActivationButtonItems.FirstOrDefault(x => x.Id == selectedItemId);
            }
        }

        public LocalizationManager Localization => _localization;

        #region Properties

        public bool EnableGyroscope
        {
            get => _enableGyroscope;
            set
            {
                if (_isReadOnlyEnableGyroscope) return;
                if (SetProperty(ref _enableGyroscope, value))
                {
                    OnPropertyChanged(nameof(IsGyroscopeActivationButtonVisible));
                    GlobalAppManager.Instance.SendCmdEnableGyroscope(value);
                }
            }
        }

        public bool AutoEnableGyroscopeOnStart
        {
            get => _autoEnableGyroscopeOnStart;
            set
            {
                if (_isReadOnlyAutoEnableGyroscopeOnStart) return;
                if (SetProperty(ref _autoEnableGyroscopeOnStart, value))
                {
                    GlobalAppManager.Instance.SendCmdAutoEnableGyroscopeOnStart(value);
                }
            }
        }

        public bool HighPrecisionGyroscope
        {
            get => _highPrecisionGyroscope;
            set
            {
                if (_isReadOnlyHighPrecisionGyroscope) return;
                if (SetProperty(ref _highPrecisionGyroscope, value))
                {
                    GlobalAppManager.Instance.SendCmdHighPrecisionGyroscope(value);
                }
            }
        }

        public bool DisableBoschAccelerometer
        {
            get => _disableBoschAccelerometer;
            set
            {
                if (_isReadOnlyDisableBoschAccelerometer) return;
                if (SetProperty(ref _disableBoschAccelerometer, value))
                {
                    GlobalAppManager.Instance.SendCmdDisableBoschAccelerometer(value);
                }
            }
        }

        public ObservableCollection<ComboBoxItemModel> GyroscopeActivationButtonItems
        {
            get => _gyroscopeActivationButtonItems;
            private set => SetProperty(ref _gyroscopeActivationButtonItems, value);
        }

        public ComboBoxItemModel GyroscopeActivationButtonSelectedItem
        {
            get => _gyroscopeActivationButtonSelectedItem;
            set
            {
                if (_isReadOnlyGyroscopeActivationButton)
                {
                    OnPropertyChanged(nameof(GyroscopeActivationButtonSelectedItem));
                    return;
                }
                
                if (SetProperty(ref _gyroscopeActivationButtonSelectedItem, value))
                {
                    GlobalAppManager.Instance.SendCmdGyroscopeActivationButton(value?.Value ?? 0);
                }
            }
        }

        public bool IsReadOnlyEnableGyroscope
        {
            get => _isReadOnlyEnableGyroscope;
            set => SetProperty(ref _isReadOnlyEnableGyroscope, value);
        }

        public bool IsReadOnlyAutoEnableGyroscopeOnStart
        {
            get => _isReadOnlyAutoEnableGyroscopeOnStart;
            set => SetProperty(ref _isReadOnlyAutoEnableGyroscopeOnStart, value);
        }

        public bool IsReadOnlyHighPrecisionGyroscope
        {
            get => _isReadOnlyHighPrecisionGyroscope;
            set => SetProperty(ref _isReadOnlyHighPrecisionGyroscope, value);
        }

        public bool IsReadOnlyDisableBoschAccelerometer
        {
            get => _isReadOnlyDisableBoschAccelerometer;
            set => SetProperty(ref _isReadOnlyDisableBoschAccelerometer, value);
        }

        public bool IsReadOnlyGyroscopeActivationButton
        {
            get => _isReadOnlyGyroscopeActivationButton;
            set => SetProperty(ref _isReadOnlyGyroscopeActivationButton, value);
        }

        #endregion

        #region Visibility Properties

        public bool IsGyroscopeActivationButtonVisible => _enableGyroscope;

        #endregion

        #region Methods

        private void InitializeGyroscopeActivationButtonItems()
        {
            GyroscopeActivationButtonItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { Id = "NotSet", DisplayName = _localization["NotSet"], Value = 0 },
                new ComboBoxItemModel { Id = "LeftTrigger", DisplayName = _localization["LeftTrigger"], Value = 1 },
                new ComboBoxItemModel { Id = "RightTrigger", DisplayName = _localization["RightTrigger"], Value = 2 },
                new ComboBoxItemModel { Id = "LeftOrRightTrigger", DisplayName = _localization["LeftOrRightTrigger"], Value = 3 }
            };

            // Restore selection if it exists
            if (GyroscopeActivationButtonSelectedItem == null ||
                !GyroscopeActivationButtonItems.Contains(GyroscopeActivationButtonSelectedItem))
            {
                GyroscopeActivationButtonSelectedItem = GyroscopeActivationButtonItems?[0]; // Default to first item
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
