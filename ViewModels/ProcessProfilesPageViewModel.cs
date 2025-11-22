using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows11Settings.Managers;
using Windows11Settings.Resources.Localization;
using Windows11Settings.ViewModels;

namespace Windows11Settings.ViewModels.Pages
{
    public class ProcessProfilesPageViewModel : INotifyPropertyChanged
    {
        private LocalizationManager _localization = LocalizationManager.Instance;
        
        private bool _keepLastProcessProfile = false;
        private ObservableCollection<ComboBoxItemModel> _profilesListItems;
        private ComboBoxItemModel _profilesListSelectedItem;
        private ObservableCollection<ComboBoxItemModel> _processListItems;
        private ComboBoxItemModel _processListSelectedItem;
        private string _currentGlobalProfile = string.Empty;
        private string _currentProcessProfile = string.Empty;

        private string _currentActiveProfile = string.Empty;
        private string _currentEditingProfile = string.Empty;
        private bool _isReadOnlyKeepLastProcessProfile = false;
        private bool _isReadOnlyProfilesListSelectedItem = false;
        private bool _isReadOnlyProcessListSelectedItem = false;
        private bool _isReadOnlyRemoveProfileCommand = false;
        private bool _isReadOnlyResetProfileCommand = false;
        private bool _isReadOnlyAddProfileCommand = false;
        private bool _isReadOnlyApplyProfileCommand = false;
        private bool _isReadOnlyAddProcessCommand = false;
        private bool _isReadOnlyRemoveProcessCommand = false;
        private bool _isReadOnlyApplyProcessCommand = false;

        public ProcessProfilesPageViewModel()
        {
            InitializeProfilesList();
            InitializeProcessList();
            
            RemoveProfileCommand = new RelayCommand(RemoveProfile, CanRemoveProfile);
            ResetProfileCommand = new RelayCommand(ResetProfile);
            AddProfileCommand = new RelayCommand(async _ => await AddProfileAsync());
            ApplyProfileCommand = new RelayCommand(ApplyProfile);
            AddProcessCommand = new RelayCommand(AddProcess);
            RemoveProcessCommand = new RelayCommand(RemoveProcess, CanRemoveProcess);
            ApplyProcessCommand = new RelayCommand(ApplyProcess);

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public void RefreshTranslations()
        {
            // Refresh default profile name
            OnPropertyChanged(nameof(ProfilesListItems));

        }

        public LocalizationManager Localization => _localization;

        // Delegate for requesting profile name from UI
        public Func<Task<string>> RequestProfileName { get; set; }

        #region Properties

        public bool KeepLastProcessProfile
        {
            get => _keepLastProcessProfile;
            set
            {
                if (_isReadOnlyKeepLastProcessProfile) return;
                var oldValue = _keepLastProcessProfile;
                if (SetProperty(ref _keepLastProcessProfile, value))
                {
                    GlobalAppManager.Instance.SendCmdKeepLastProcessProfile(value);
                }
            }
        }

        public ObservableCollection<ComboBoxItemModel> ProfilesListItems
        {
            get => _profilesListItems;
            private set => SetProperty(ref _profilesListItems, value);
        }

        public ComboBoxItemModel ProfilesListSelectedItem
        {
            get => _profilesListSelectedItem;
            set
            {
                if (_isReadOnlyProfilesListSelectedItem) return;
                var oldValue = _profilesListSelectedItem;
                if (SetProperty(ref _profilesListSelectedItem, value))
                {
                    ((RelayCommand)RemoveProfileCommand).RaiseCanExecuteChanged();
                    GlobalAppManager.Instance.SendCmdProfilesListSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public ObservableCollection<ComboBoxItemModel> ProcessListItems
        {
            get => _processListItems;
            private set => SetProperty(ref _processListItems, value);
        }

        public ComboBoxItemModel ProcessListSelectedItem
        {
            get => _processListSelectedItem;
            set
            {
                if (_isReadOnlyProcessListSelectedItem) return;
                var oldValue = _processListSelectedItem;
                if (SetProperty(ref _processListSelectedItem, value))
                {
                    ((RelayCommand)RemoveProcessCommand).RaiseCanExecuteChanged();
                    GlobalAppManager.Instance.SendCmdProcessListSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public string CurrentGlobalProfile
        {
            get => _currentGlobalProfile;
            set
            {
                SetProperty(ref _currentGlobalProfile, value);
                SetActiveComboboxesItem(value);
            }
        }

        private void SetActiveComboboxesItem(string value)
        {
            if (value.Length > 0)
            {
                var newValue = new ComboBoxItemModel
                {
                    DisplayName = value,
                    Id = value
                };

                if (ProfilesListItems.Contains(newValue))
                {
                    ProfilesListSelectedItem = newValue;
                }
                else if (ProcessListItems.Contains(newValue))
                {
                    ProcessListSelectedItem = newValue;
                }
            }
        }

        public string CurrentProcessProfile
        {
            get => _currentProcessProfile;
            set
            {
                SetProperty(ref _currentProcessProfile, value);
                SetActiveComboboxesItem(value);
            }
        }

        public string CurrentActiveProfile
        {
            get => _currentActiveProfile;
            set
            {
                SetProperty(ref _currentActiveProfile, value);
                CurrentGlobalProfile = value;
            }
        }

        public string CurrentEditingProfile
        {
            get => _currentEditingProfile;
            set
            {
                SetProperty(ref _currentEditingProfile, value);
                SetActiveComboboxesItem(value);
            }
        }

        #endregion

        #region IsReadOnly Properties

        public bool IsReadOnlyKeepLastProcessProfile
        {
            get => _isReadOnlyKeepLastProcessProfile;
            set => SetProperty(ref _isReadOnlyKeepLastProcessProfile, value);
        }

        public bool IsReadOnlyProfilesListSelectedItem
        {
            get => _isReadOnlyProfilesListSelectedItem;
            set => SetProperty(ref _isReadOnlyProfilesListSelectedItem, value);
        }

        public bool IsReadOnlyProcessListSelectedItem
        {
            get => _isReadOnlyProcessListSelectedItem;
            set => SetProperty(ref _isReadOnlyProcessListSelectedItem, value);
        }

        public bool IsReadOnlyRemoveProfileCommand
        {
            get => _isReadOnlyRemoveProfileCommand;
            set => SetProperty(ref _isReadOnlyRemoveProfileCommand, value);
        }

        public bool IsReadOnlyResetProfileCommand
        {
            get => _isReadOnlyResetProfileCommand;
            set => SetProperty(ref _isReadOnlyResetProfileCommand, value);
        }

        public bool IsReadOnlyAddProfileCommand
        {
            get => _isReadOnlyAddProfileCommand;
            set => SetProperty(ref _isReadOnlyAddProfileCommand, value);
        }

        public bool IsReadOnlyApplyProfileCommand
        {
            get => _isReadOnlyApplyProfileCommand;
            set => SetProperty(ref _isReadOnlyApplyProfileCommand, value);
        }

        public bool IsReadOnlyAddProcessCommand
        {
            get => _isReadOnlyAddProcessCommand;
            set => SetProperty(ref _isReadOnlyAddProcessCommand, value);
        }

        public bool IsReadOnlyRemoveProcessCommand
        {
            get => _isReadOnlyRemoveProcessCommand;
            set => SetProperty(ref _isReadOnlyRemoveProcessCommand, value);
        }

        public bool IsReadOnlyApplyProcessCommand
        {
            get => _isReadOnlyApplyProcessCommand;
            set => SetProperty(ref _isReadOnlyApplyProcessCommand, value);
        }

        #endregion

        #region Commands

        public ICommand RemoveProfileCommand { get; }
        public ICommand ResetProfileCommand { get; }
        public ICommand AddProfileCommand { get; }
        public ICommand ApplyProfileCommand { get; }
        public ICommand AddProcessCommand { get; }
        public ICommand RemoveProcessCommand { get; }
        public ICommand ApplyProcessCommand { get; }

        private void RemoveProfile(object parameter)
        {
            if (ProfilesListSelectedItem != null)
            {
                GlobalAppManager.Instance.SendCmdRemoveGlobalProfile(ProfilesListSelectedItem.Id);
            }
        }

        private bool CanRemoveProfile(object parameter)
        {
            return ProfilesListSelectedItem != null;
        }

        private void ResetProfile(object parameter)
        {
            if (ProfilesListSelectedItem != null)
            {
                GlobalAppManager.Instance.SendCmdResetGlobalProfile(ProfilesListSelectedItem.Id);
            }
        }

        private async Task AddProfileAsync()
        {
            if (RequestProfileName != null)
            {
                var profileName = await RequestProfileName();

                if (!string.IsNullOrWhiteSpace(profileName))
                {
                    GlobalAppManager.Instance.SendCmdAddGlobalProfile(profileName);
                }
            }
        }


        public void SetProfiles(List<string> profiles)
        {
            ProfilesListItems.Clear();
            foreach (var name in profiles)
            {
                AddProfile(name);
            }    
        }


        public void SetProcesses(List<string> processes)
        {
            ProcessListItems.Clear();
            foreach (var name in processes)
            {
                AddProcessByName(name);
            }
        }

        public void AddProfile(string profileName)
        {
            if (!string.IsNullOrWhiteSpace(profileName))
            {
                var newProfile = new ComboBoxItemModel
                {
                    DisplayName = profileName,
                    Id = profileName
                };

                if (!ProfilesListItems.Contains(newProfile))
                {
                    ProfilesListItems.Add(newProfile);
                    ProfilesListSelectedItem = newProfile;
                }
            }
        }

        private void ApplyProfile(object parameter)
        {
            if (ProfilesListSelectedItem != null)
            {
                GlobalAppManager.Instance.SendCmdApplyGlobalProfile(ProfilesListSelectedItem.Id);
            }
        }

        private void AddProcessByName(string name)
        {
            // In a real implementation, this would show a file browser or process selector
            var processName = name;
            var newProcess = new ComboBoxItemModel
            {
                DisplayName = processName,
                Id = processName
            };
            
            if (!ProcessListItems.Contains(newProcess))
            {
                ProcessListItems.Add(newProcess);
                ProcessListSelectedItem = newProcess;
            }
        }


        private void AddProcess(object parameter)
        {
            GlobalAppManager.Instance.SendCmdAddProcessProfile(true);
        }

        private void RemoveProcess(object parameter)
        {
            if (ProcessListSelectedItem != null)
            {
                GlobalAppManager.Instance.SendCmdRemoveProcessProfile(ProcessListSelectedItem.Id);
            }
        }

        private bool CanRemoveProcess(object parameter)
        {
            return ProcessListSelectedItem != null;
        }

        private void ApplyProcess(object parameter)
        {
            if (ProcessListSelectedItem != null)
            {
                GlobalAppManager.Instance.SendCmdEditProcessProfile(ProcessListSelectedItem.Id);
            }
        }

        #endregion

        #region Methods

        private void InitializeProfilesList()
        {
            _profilesListItems = new ObservableCollection<ComboBoxItemModel>();
            _profilesListSelectedItem = null;
        }

        private void InitializeProcessList()
        {
            _processListItems = new ObservableCollection<ComboBoxItemModel>();
            _processListSelectedItem = null;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string    propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
