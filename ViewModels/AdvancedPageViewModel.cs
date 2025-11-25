using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows11Settings.Managers;
using Windows11Settings.Resources.Localization;
using Windows11Settings.ViewModels;
using Windows11Settings.Models;

namespace Windows11Settings.ViewModels.Pages
{
    public class AdvancedPageViewModel : INotifyPropertyChanged
    {
        private readonly LocalizationManager _localization;
        private bool _addToSystemAutorun;
        private bool _disableSystemMonitoring;
        private bool _disableBluetoothInSleepMode;
        private bool _useNewInterface = true;
        private bool _minimizeToSystemTray;
        private ComboBoxItemModel _windowSizeSelectedItem;
        private string _maVersion = "Motion Assistant";
        private string _modVersion = "Power Mod";
        private ComboBoxItemModel _selectedLanguage;

        // Read-only state properties for controls
        private bool _isReadOnlyAddToSystemAutorun = false;
        private bool _isReadOnlyDisableSystemMonitoring = false;
        private bool _isReadOnlyDisableBluetoothInSleepMode = false;
        private bool _isReadOnlyUseNewInterface = false;
        private bool _isReadOnlyMinimizeToSystemTray = false;
        private bool _isReadOnlyWindowSizeSelectedItem = false;
        private bool _isReadOnlyCheckForUpdatesCommand = false;

        public AdvancedPageViewModel()
        {
            _localization = LocalizationManager.Instance;
            _localization.PropertyChanged += (s, e) => RefreshTranslations();

            InitializeWindowSizeItems();
            InitializeLanguageItems();
            CheckForUpdatesCommand = new RelayCommand(_ => CheckForUpdates());

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public LocalizationManager Localization => _localization;

        public ObservableCollection<ComboBoxItemModel> LanguageItems { get; } = new ObservableCollection<ComboBoxItemModel>();

        public ComboBoxItemModel SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    var oldValue = _selectedLanguage;
                    SetProperty(ref _selectedLanguage, value);
                    
                    if (oldValue != value && !string.IsNullOrEmpty(value.Id))
                    {
                        // Update the localization manager's current language
                        _localization.CurrentLanguage = value.Id;
                        GlobalSettings.Instance.CurrentLanguage = value.Id;
                    }
                }
            }
        }

        public bool AddToSystemAutorun
        {
            get => _addToSystemAutorun;
            set
            {
                if (IsReadOnlyAddToSystemAutorun)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(AddToSystemAutorun));
                    return;
                }

                var oldValue = _addToSystemAutorun;
                SetProperty(ref _addToSystemAutorun, value);

                if (oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdAddToSystemAutorun(value);
                }
            }
        }

        public bool DisableSystemMonitoring
        {
            get => _disableSystemMonitoring;
            set
            {
                if (IsReadOnlyDisableSystemMonitoring)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(DisableSystemMonitoring));
                    return;
                }

                var oldValue = _disableSystemMonitoring;
                SetProperty(ref _disableSystemMonitoring, value);

                if (oldValue != value)
                {
                    //GlobalAppManager.Instance.SendCmdDisableSystemMonitoring(value);
                }
            }
        }

        public bool DisableBluetoothInSleepMode
        {
            get => _disableBluetoothInSleepMode;
            set
            {
                if (IsReadOnlyDisableBluetoothInSleepMode)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(DisableBluetoothInSleepMode));
                    return;
                }

                var oldValue = _disableBluetoothInSleepMode;
                SetProperty(ref _disableBluetoothInSleepMode, value);

                if (oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdDisableBluetoothInSleepMode(value);
                }
            }
        }

        public bool UseNewInterface
        {
            get => _useNewInterface;
            set
            {
                if (IsReadOnlyUseNewInterface)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(UseNewInterface));
                    return;
                }

                var oldValue = _useNewInterface;
                SetProperty(ref _useNewInterface, value);

                if (oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdUseNewInterface(value);
                }
            }
        }

        public bool MinimizeToSystemTray
        {
            get => _minimizeToSystemTray;
            set
            {
                if (IsReadOnlyMinimizeToSystemTray)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(MinimizeToSystemTray));
                    return;
                }

                var oldValue = _minimizeToSystemTray;
                SetProperty(ref _minimizeToSystemTray, value);

                if (oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdMinimizeToSystemTray(value);
                }
            }
        }

        public void SetAppVersions(string input)
        {
            if (string.IsNullOrEmpty(input))
                return;

            // Remove the [default] or any [...] suffix
            int bracketIndex = input.IndexOf('[');
            if (bracketIndex > 0)
            {
                input = input.Substring(0, bracketIndex).Trim();
            }

            // Split by '+' 
            string[] parts = input.Split('+');

            if (parts.Length < 2)
                return;

            _maVersion = parts[0].Trim();
            _modVersion = parts[1].Trim();

            OnPropertyChanged(nameof(MAVersion));
            OnPropertyChanged(nameof(ModVersion));
        }

        public string MAVersion
        {
            get => _maVersion;
        }

        public string ModVersion
        {
            get => _modVersion;
        }

        public ObservableCollection<ComboBoxItemModel> WindowSizeItems { get; } = new ObservableCollection<ComboBoxItemModel>();

        public ComboBoxItemModel WindowSizeSelectedItem
        {
            get => _windowSizeSelectedItem;
            set
            {
                if (IsReadOnlyWindowSizeSelectedItem)
                {
                    // Control is read-only, don't allow changes
                    OnPropertyChanged(nameof(WindowSizeSelectedItem));
                    return;
                }

                var oldValue = _windowSizeSelectedItem;
                SetProperty(ref _windowSizeSelectedItem, value);

                if (oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdWindowSizeSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        #region Read-Only Control Properties

        public bool IsReadOnlyAddToSystemAutorun
        {
            get => _isReadOnlyAddToSystemAutorun;
            set => SetProperty(ref _isReadOnlyAddToSystemAutorun, value);
        }

        public bool IsReadOnlyDisableSystemMonitoring
        {
            get => _isReadOnlyDisableSystemMonitoring;
            set => SetProperty(ref _isReadOnlyDisableSystemMonitoring, value);
        }

        public bool IsReadOnlyDisableBluetoothInSleepMode
        {
            get => _isReadOnlyDisableBluetoothInSleepMode;
            set => SetProperty(ref _isReadOnlyDisableBluetoothInSleepMode, value);
        }

        public bool IsReadOnlyUseNewInterface
        {
            get => _isReadOnlyUseNewInterface;
            set => SetProperty(ref _isReadOnlyUseNewInterface, value);
        }

        public bool IsReadOnlyMinimizeToSystemTray
        {
            get => _isReadOnlyMinimizeToSystemTray;
            set => SetProperty(ref _isReadOnlyMinimizeToSystemTray, value);
        }

        public bool IsReadOnlyWindowSizeSelectedItem
        {
            get => _isReadOnlyWindowSizeSelectedItem;
            set => SetProperty(ref _isReadOnlyWindowSizeSelectedItem, value);
        }

        public bool IsReadOnlyCheckForUpdatesCommand
        {
            get => _isReadOnlyCheckForUpdatesCommand;
            set => SetProperty(ref _isReadOnlyCheckForUpdatesCommand, value);
        }

        #endregion

        public ICommand CheckForUpdatesCommand { get; }

        private void InitializeWindowSizeItems()
        {
            WindowSizeItems.Clear();
            WindowSizeItems.Add(new ComboBoxItemModel { DisplayName = _localization["WindowSizeNormal"], Id = "Normal" });
            WindowSizeItems.Add(new ComboBoxItemModel { DisplayName = _localization["WindowSizeMinimized"], Id = "Minimum" });
            WindowSizeItems.Add(new ComboBoxItemModel { DisplayName = _localization["WindowSizeMaximized"], Id = "Maximum" });

            if (WindowSizeSelectedItem == null)
            {
                WindowSizeSelectedItem = WindowSizeItems[0];
            }
        }

        private void InitializeLanguageItems()
        {
            LanguageItems.Clear();
            
            // Load available languages from the localization manager
            foreach (var kvp in _localization.AvailableLanguages)
            {
                LanguageItems.Add(new ComboBoxItemModel 
                { 
                    DisplayName = kvp.Value, 
                    Id = kvp.Key 
                });
            }

            // Set the current language
            if (SelectedLanguage == null)
            {
                var newSelectedLanguage = LanguageItems.FirstOrDefault(item => item.Id == _localization.CurrentLanguage);
                if (newSelectedLanguage != null)
                {
                    SelectedLanguage = newSelectedLanguage;
                }
            }

            OnPropertyChanged(nameof(SelectedLanguage));
        }

        private void RefreshTranslations()
        {
            var currentWindowItem = WindowSizeSelectedItem;
            var currentLanguageItem = SelectedLanguage;
            
            InitializeWindowSizeItems();
            
            // Restore window size selection
            if (currentWindowItem != null)
            {
                var newWindowItem = WindowSizeItems.FirstOrDefault(item => item.Id == currentWindowItem.Id);
                if (newWindowItem != null)
                {
                    WindowSizeSelectedItem = newWindowItem;
                }
            }

        }

        private void CheckForUpdates()
        {
            try
            {
                // Open GitHub releases page
                var psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/ThatUsernameAlreadyExist/Motion-Assistant-Power-Mod/releases",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Handle error silently or log it
                System.Diagnostics.Debug.WriteLine($"Error opening GitHub: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
