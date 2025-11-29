using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using PmGui.Managers;
using PmGui.Resources.Localization;
using PmGui.ViewModels;

namespace PmGui.ViewModels.Pages
{
    public class OSDOverlayPageViewModel : INotifyPropertyChanged
    {
        private LocalizationManager _localization = LocalizationManager.Instance;
        
        private bool _enableOSDOverlay = false;
        private ObservableCollection<ComboBoxItemModel> _osdTypeItems;
        private ComboBoxItemModel _osdTypeSelectedItem;
        private bool _isReadOnlyEnableOSDOverlay = false;
        private bool _isReadOnlyOSDTypeSelectedItem = false;
        private bool _isReadOnlyEnableDownloadRTSS = false;

        public OSDOverlayPageViewModel()
        {
            _localization.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(Localization));
                RefreshTranslations();
            };
            
            InitializeOSDTypeItems();

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public void RefreshTranslations()
        {
            // Refresh combo box items with new translations
            var selectedType = OSDTypeSelectedItem;
            InitializeOSDTypeItems();
            OSDTypeSelectedItem = selectedType != null ? OSDTypeItems.FirstOrDefault(item => item.Id == selectedType.Id) : OSDTypeItems.FirstOrDefault();
        }

        public LocalizationManager Localization => _localization;

        #region Properties

        public bool EnableOSDOverlay
        {
            get => _enableOSDOverlay;
            set
            {
                if (IsReadOnlyEnableOSDOverlay)
                    return;
                    
                var oldValue = _enableOSDOverlay;
                if (SetProperty(ref _enableOSDOverlay, value) && oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdEnableOSDOverlay(value);
                }
            }
        }

        public ObservableCollection<ComboBoxItemModel> OSDTypeItems
        {
            get => _osdTypeItems;
            private set => SetProperty(ref _osdTypeItems, value);
        }

        public ComboBoxItemModel OSDTypeSelectedItem
        {
            get => _osdTypeSelectedItem;
            set
            {
                if (IsReadOnlyOSDTypeSelectedItem)
                    return;
                    
                var oldValue = _osdTypeSelectedItem;
                if (SetProperty(ref _osdTypeSelectedItem, value) && oldValue != value)
                {
                    GlobalAppManager.Instance.SendCmdOSDTypeSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        #endregion

        #region IsReadOnly Properties

        public bool IsReadOnlyEnableOSDOverlay
        {
            get => _isReadOnlyEnableOSDOverlay;
            set => SetProperty(ref _isReadOnlyEnableOSDOverlay, value);
        }

        public bool IsReadOnlyOSDTypeSelectedItem
        {
            get => _isReadOnlyOSDTypeSelectedItem;
            set => SetProperty(ref _isReadOnlyOSDTypeSelectedItem, value);
        }

        public bool IsReadOnlyEnableDownloadRTSS
        {
            get => _isReadOnlyEnableDownloadRTSS;
            set => SetProperty(ref _isReadOnlyEnableDownloadRTSS, value);
        }

        #endregion

        #region Methods

        private void InitializeOSDTypeItems()
        {
            _osdTypeItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { DisplayName = _localization["OSDTypeFull"], Id = "full" },
                new ComboBoxItemModel { DisplayName = _localization["OSDTypeOneLine"], Id = "oneline" },
                new ComboBoxItemModel { DisplayName = _localization["OSDTypeSimple"], Id = "simple" }
            };
            
            // Set default selection
            if (_osdTypeSelectedItem == null || !_osdTypeItems.Contains(_osdTypeSelectedItem))
            {
                _osdTypeSelectedItem = _osdTypeItems[0];
            }
            
            OnPropertyChanged(nameof(OSDTypeItems));
        }

        public void DownloadRTSS()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _localization["RTSSUrl"],
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Handle exception if needed
                System.Diagnostics.Debug.WriteLine($"Failed to open RTSS download page: {ex.Message}");
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