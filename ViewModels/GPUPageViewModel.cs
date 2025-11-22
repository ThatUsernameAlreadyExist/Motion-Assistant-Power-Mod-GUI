// ViewModels/GPUPageViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows11Settings.Managers;
using Windows11Settings.Resources.Localization;
using Windows11Settings.ViewModels;

namespace Windows11Settings.ViewModels.Pages
{
    public class GPUPageViewModel : INotifyPropertyChanged
    {
        private LocalizationManager _localization = LocalizationManager.Instance;
        
        // GPU Model
        private string _gpuModel = "GPU";
        
        // GPU optimization settings
        private bool _optimizeGpuClocksEnabled = false;
        private ComboBoxItemModel _optimizationModeSelectedItem;
        private bool _customGpuClocksRangeEnabled = false;
        
        // GPU clock range
        private double _minGpuClockValue = 400;
        private double _maxGpuClockValue = 3000;
        
        // Slider constraints
        private double _gpuClockMinValue = 400;
        private double _gpuClockMaxValue = 3000;
        
        // Optimization mode options
        private ObservableCollection<ComboBoxItemModel> _optimizationModeItems;
        
        // Readonly flags
        private bool _isReadOnlyMinGpuClockValue = false;
        private bool _isReadOnlyMaxGpuClockValue = false;
        private bool _isReadOnlyCustomGpuClocksRangeEnabled = false;
        private bool _isReadOnlyOptimizationModeSelectedItem = false;
        private bool _isReadOnlyOptimizeGpuClocksEnabled = false;
        private bool _isReadOnlyApplyGpuRange = false;
        
        // Commands
        private ICommand _applyGpuRangeCommand;
        private ICommand _resetGpuCommand;

        public GPUPageViewModel()
        {
            // Subscribe to language changes to refresh ComboBox items
            _localization.PropertyChanged += (s, e) => RefreshTranslations();
            
            InitializeOptimizationModeItems();

            GlobalAppManager.Instance.RegisterPageViewModel(this);
        }

        public void RefreshTranslations()
        {
            // Refresh combo box items with new translations
            var selectedModeId = OptimizationModeSelectedItem?.Id ?? string.Empty;
            InitializeOptimizationModeItems();

            // Restore selection by ID if it exists
            if (!string.IsNullOrEmpty(selectedModeId) && OptimizationModeItems != null)
            {
                OptimizationModeSelectedItem = OptimizationModeItems.FirstOrDefault(x => x.Id == selectedModeId);
            }
        }

        public LocalizationManager Localization => _localization;

        #region Properties

        public string GPUModel
        {
            get => _gpuModel;
            set => SetProperty(ref _gpuModel, value);
        }

        public bool OptimizeGpuClocksEnabled
        {
            get => _optimizeGpuClocksEnabled;
            set
            {
                if (IsReadOnlyOptimizeGpuClocksEnabled)
                {
                    OnPropertyChanged(nameof(OptimizeGpuClocksEnabled));
                    return;
                }
                
                var oldValue = _optimizeGpuClocksEnabled;
                if (SetProperty(ref _optimizeGpuClocksEnabled, value))
                {
                    OnPropertyChanged(nameof(IsCustomGpuClockRangeEnableVisible));
                    OnPropertyChanged(nameof(IsCustomGpuClockRangeVisible));

                    if (oldValue != value)
                    {
                        GlobalAppManager.Instance.SendCmdOptimizeGpuClocksEnabled(value);
                    }
                }
            }
        }

        public ComboBoxItemModel OptimizationModeSelectedItem
        {
            get => _optimizationModeSelectedItem;
            set
            {
                if (IsReadOnlyOptimizationModeSelectedItem)
                {
                    OnPropertyChanged(nameof(OptimizationModeSelectedItem));
                    return;
                }
                
                if (SetProperty(ref _optimizationModeSelectedItem, value))
                {
                    OnPropertyChanged(nameof(IsMinGpuClockSliderVisible));
                    OnPropertyChanged(nameof(IsMaxGpuClockSliderVisible));
                    OnPropertyChanged(nameof(IsCustomGpuClockRangeEnableVisible));
                    OnPropertyChanged(nameof(IsApplyRangeButtonVisible));
                    OnPropertyChanged(nameof(IsCustomGpuClockRangeVisible));

                    GlobalAppManager.Instance.SendCmdOptimizationModeSelectedItem(value?.Id ?? string.Empty);
                }
            }
        }

        public bool CustomGpuClocksRangeEnabled
        {
            get => _customGpuClocksRangeEnabled;
            set
            {
                if (IsReadOnlyCustomGpuClocksRangeEnabled)
                {
                    OnPropertyChanged(nameof(CustomGpuClocksRangeEnabled));
                    return;
                }
                
                var oldValue = _customGpuClocksRangeEnabled;
                if (SetProperty(ref _customGpuClocksRangeEnabled, value))
                {
                    OnPropertyChanged(nameof(IsMinGpuClockSliderVisible));
                    OnPropertyChanged(nameof(IsMaxGpuClockSliderVisible));
                    OnPropertyChanged(nameof(IsCustomGpuClockRangeEnableVisible));
                    OnPropertyChanged(nameof(IsApplyRangeButtonVisible));
                    OnPropertyChanged(nameof(IsCustomGpuClockRangeVisible));
                    if (oldValue != value)
                    {
                        GlobalAppManager.Instance.SendCmdCustomGpuClocksRangeEnabled(value);
                    }
                }
            }
        }

        public double MinGpuClockValue
        {
            get => _minGpuClockValue;
            set
            {
                if (IsReadOnlyMinGpuClockValue)
                {
                    OnPropertyChanged(nameof(MinGpuClockValue));
                    return;
                }


                var toSet = Math.Max(value, GpuClockMinValue);

                if (SetProperty(ref _minGpuClockValue, value))
                {
                    // Ensure min value is always less than max value
                    if (value > _maxGpuClockValue)
                    {
                        MaxGpuClockValue = value;
                    }
                    // GlobalAppManager command call removed - optimization
                }
            }
        }

        public double MaxGpuClockValue
        {
            get => _maxGpuClockValue;
            set
            {
                if (IsReadOnlyMaxGpuClockValue)
                {
                    OnPropertyChanged(nameof(MaxGpuClockValue));
                    return;
                }

                var toSet = Math.Min(value, GpuClockMaxValue);
                
                if (SetProperty(ref _maxGpuClockValue, toSet))
                {
                    // Ensure max value is always greater than min value
                    if (value < _minGpuClockValue)
                    {
                        MinGpuClockValue = value;
                    }

                        
                    // GlobalAppManager command call removed - optimization
                }
            }
        }

        public double GpuClockMinValue
        {
            get => _gpuClockMinValue;
            set => SetProperty(ref _gpuClockMinValue, value);
        }

        public double GpuClockMaxValue
        {
            get => _gpuClockMaxValue;
            set => SetProperty(ref _gpuClockMaxValue, value);
        }

        #endregion

        #region ReadOnly Properties

        public bool IsReadOnlyMinGpuClockValue
        {
            get => _isReadOnlyMinGpuClockValue;
            set => SetProperty(ref _isReadOnlyMinGpuClockValue, value);
        }

        public bool IsReadOnlyMaxGpuClockValue
        {
            get => _isReadOnlyMaxGpuClockValue;
            set => SetProperty(ref _isReadOnlyMaxGpuClockValue, value);
        }

        public bool IsReadOnlyCustomGpuClocksRangeEnabled
        {
            get => _isReadOnlyCustomGpuClocksRangeEnabled;
            set => SetProperty(ref _isReadOnlyCustomGpuClocksRangeEnabled, value);
        }

        public bool IsReadOnlyOptimizationModeSelectedItem
        {
            get => _isReadOnlyOptimizationModeSelectedItem;
            set => SetProperty(ref _isReadOnlyOptimizationModeSelectedItem, value);
        }

        public bool IsReadOnlyOptimizeGpuClocksEnabled
        {
            get => _isReadOnlyOptimizeGpuClocksEnabled;
            set => SetProperty(ref _isReadOnlyOptimizeGpuClocksEnabled, value);
        }

        public bool IsReadOnlyApplyGpuRange
        {
            get => _isReadOnlyApplyGpuRange;
            set => SetProperty(ref _isReadOnlyApplyGpuRange, value);
        }

        #endregion

        #region Visibility Properties

        public bool IsMinGpuClockSliderVisible => _customGpuClocksRangeEnabled;
        public bool IsMaxGpuClockSliderVisible => _customGpuClocksRangeEnabled;

        public bool IsCustomGpuClockRangeEnableVisible => _optimizeGpuClocksEnabled && OptimizationModeSelectedItem?.Id == "Auto";
        public bool IsCustomGpuClockRangeVisible => _customGpuClocksRangeEnabled && IsCustomGpuClockRangeEnableVisible;

        public bool IsApplyRangeButtonVisible => _customGpuClocksRangeEnabled && IsCustomGpuClockRangeVisible;

        #endregion

        #region ComboBox Items

        public ObservableCollection<ComboBoxItemModel> OptimizationModeItems
        {
            get => _optimizationModeItems;
            private set => SetProperty(ref _optimizationModeItems, value);
        }

        #endregion

        #region Commands

        public ICommand ApplyGpuRangeCommand
        {
            get => _applyGpuRangeCommand ?? (_applyGpuRangeCommand= new RelayCommand(ApplyGpuRange));
        }

        public ICommand ResetGpuCommand
        {
            get => _resetGpuCommand ?? (_resetGpuCommand = new RelayCommand(ResetGpu));
        }

        private void ApplyGpuRange(object parameter)
        {
            // Apply custom GPU clock range
            GlobalAppManager.Instance.SendCmdApplyCustomGpuClocks(true);
        }

        private void ResetGpu(object parameter)
        {
            GlobalAppManager.Instance.SendCmdResetGpu(true);
        }

        #endregion

        #region Methods

        private void InitializeOptimizationModeItems()
        {
            OptimizationModeItems = new ObservableCollection<ComboBoxItemModel>
            {
                new ComboBoxItemModel { Id = "Balanced", DisplayName = _localization["FixedClock"], Value = 0 },
                new ComboBoxItemModel { Id = "Auto", DisplayName = _localization["AdaptiveClock"], Value = 1 }
            };

            // Restore selection if it exists
            if (OptimizationModeSelectedItem == null ||
                !OptimizationModeItems.Contains(OptimizationModeSelectedItem))
            {
                OptimizationModeSelectedItem = OptimizationModeItems?[0]; // Default to first item
            }
        }

        public void UpdateGpuClockRange(int minClock, int maxClock)
        {
            GpuClockMinValue = minClock;
            GpuClockMaxValue = maxClock;
            
            // Adjust current values if they're out of new range
            if (MinGpuClockValue < minClock) MinGpuClockValue = minClock;
            if (MaxGpuClockValue > maxClock) MaxGpuClockValue = maxClock;
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
