using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows11Settings.Models;
using Windows11Settings.Resources.Localization;
using Windows11Settings.ViewModels.Pages;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Windows11Settings.Managers;

namespace Windows11Settings.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _selectedPage = "Monitoring";
        private bool _isMenuExpanded = true;
        private bool _isDarkTheme = false;
        private LocalizationManager _localization;
        private const double MinPanelWidth = 200;
        private const double MaxPanelWidth = 400;
        private const double CollapsedWidth = 68;
        private const double IconWidth = 16;
        private const double IconSpacing = 12;
        private const double ButtonPadding = 24;
        private const double ExtraMargin = 32;
        private int _selectedLanguageIndex = 0;

        // Read-only state properties for controls
        private bool _isReadOnlyIsDarkTheme = false;
        private bool _isReadOnlySelectedLanguageIndex = false;

        public CPUPageViewModel CPUPageViewModel { get; } = new CPUPageViewModel();
        public GPUPageViewModel GPUPageViewModel { get; } = new GPUPageViewModel();
        public FanPageViewModel FanPageViewModel { get; } = new FanPageViewModel();
        public GyroscopePageViewModel GyroscopePageViewModel { get; } = new GyroscopePageViewModel();
        public OSDOverlayPageViewModel OSDOverlayPageViewModel { get; } = new OSDOverlayPageViewModel();
        public ProcessProfilesPageViewModel ProcessProfilesPageViewModel { get; } = new ProcessProfilesPageViewModel();
        public AdvancedPageViewModel AdvancedPageViewModel { get; } = new AdvancedPageViewModel();

        public int SelectedLanguageIndex
        {
            get => _selectedLanguageIndex;
            set
            {
                if (_selectedLanguageIndex != value)
                {
                    _selectedLanguageIndex = value;
                    OnPropertyChanged();
                    
                    var newLanguage = value == 0 ? "en" : "ru";
                    _localization.CurrentLanguage = newLanguage;
                    
                    // Save language change to GlobalSettings
                    GlobalSettings.Instance.CurrentLanguage = newLanguage;
                }
            }
        }

        public MainWindowViewModel()
        {
            _localization = LocalizationManager.Instance;
            _localization.PropertyChanged += (s, e) =>
            {
                // HACK: Force Localization property to appear "changed" by temporarily nulling it
                // This forces Avalonia to re-evaluate ALL bindings that use Localization
                var temp = _localization;
                _localization = null;
                OnPropertyChanged(nameof(Localization));
                _localization = temp;
                
                // Notify that ALL properties on this ViewModel have changed
                // This forces Avalonia to re-evaluate all bindings
                OnPropertyChanged(string.Empty);
                
                // Also explicitly notify key properties
                OnPropertyChanged(nameof(Localization));
                OnPropertyChanged(nameof(MenuWidth));
                OnPropertyChanged(nameof(CalculatedWidth));

                _selectedLanguageIndex = _localization.CurrentLanguage == "en" ? 0 : 1;
                OnPropertyChanged(nameof(SelectedLanguageIndex));
            };

            MenuItems = new ObservableCollection<SettingsMenuItem>
            {
                new SettingsMenuItem { Icon = "📈", TitleKey = "Monitoring", PageKey = "Monitoring", IsSelected = true },
                new SettingsMenuItem { Icon = "🖥️", TitleKey = "CPU", PageKey = "CPU" },
                new SettingsMenuItem { Icon = "🕹️", TitleKey = "GPU", PageKey = "GPU" },
                new SettingsMenuItem { Icon = "❄️", TitleKey = "Fan", PageKey = "Fan" },
                new SettingsMenuItem { Icon = "🔄", TitleKey = "Gyroscope", PageKey = "Gyroscope" },
                new SettingsMenuItem { Icon = "🎞️", TitleKey = "OSDOverlay", PageKey = "OSDOverlay" },
                new SettingsMenuItem { Icon = "📋", TitleKey = "ProcessProfiles", PageKey = "ProcessProfiles" },
                new SettingsMenuItem { Icon = "⚙️", TitleKey = "Advanced", PageKey = "Advanced" }
            };

            // Subscribe to profile changes to update badge
            ProcessProfilesPageViewModel.PropertyChanged += ProcessProfilesPageViewModel_PropertyChanged;

            SelectPageCommand = new RelayCommand(SelectPage);
            ToggleMenuCommand = new RelayCommand(_ => ToggleMenu());
            
            // Load settings from GlobalSettings
            ApplyGlobalSettings();
            
            UpdateThemeColors();
            
            // Register with GlobalAppManager for cross-page communication
            GlobalAppManager.Instance.MainViewModel = this;
            GlobalAppManager.Instance.RegisterPageViewModel(AdvancedPageViewModel);
        }

        public LocalizationManager Localization => _localization;
        public ObservableCollection<SettingsMenuItem> MenuItems { get; }

        public string SelectedPage
        {
            get => _selectedPage;
            set
            {
                if (_selectedPage != value)
                {
                    _selectedPage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMenuExpanded
        {
            get => _isMenuExpanded;
            set
            {
                if (_isMenuExpanded != value)
                {
                    _isMenuExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MenuWidth));
                }
            }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();
                    UpdateThemeColors();
                    
                    // Save theme change to GlobalSettings
                    GlobalSettings.Instance.IsDarkTheme = _isDarkTheme;
                }
            }
        }

        #region Read-Only Control Properties

        public bool IsReadOnlyIsDarkTheme
        {
            get => _isReadOnlyIsDarkTheme;
            set => SetProperty(ref _isReadOnlyIsDarkTheme, value);
        }

        public bool IsReadOnlySelectedLanguageIndex
        {
            get => _isReadOnlySelectedLanguageIndex;
            set => SetProperty(ref _isReadOnlySelectedLanguageIndex, value);
        }

        // Additional read-only properties for AdvancedPage
        public bool IsReadOnlyLanguage => _isReadOnlySelectedLanguageIndex;

        #endregion

        public double CalculatedWidth
        {
            get
            {
                if (!IsMenuExpanded)
                    return CollapsedWidth;

                double maxWidth = 0;

                var typeface = new Typeface("Segoe UI");
                var fontSize = 14;

                foreach (var item in MenuItems)
                {
                    var text = item.Title;
                    
                    var formattedText = new FormattedText(
                        text,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        Brushes.Black);

                    double textWidth = formattedText.Width;
                    double totalWidth = IconWidth + IconSpacing + textWidth + ButtonPadding + ExtraMargin;

                    if (totalWidth > maxWidth)
                        maxWidth = totalWidth;
                }

                var settingsText = new FormattedText(
                    _localization["Settings"],
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    24,
                    Brushes.Black);

                double settingsTitleWidth = settingsText.Width + 40;

                maxWidth = Math.Max(maxWidth, settingsTitleWidth);

                maxWidth = Math.Max(MinPanelWidth, Math.Min(MaxPanelWidth, maxWidth));

                return maxWidth + 10;
            }
        }

        public double MenuWidth => CalculatedWidth;

        public ICommand SelectPageCommand { get; }
        public ICommand ToggleMenuCommand { get; }

        private void SelectPage(object parameter)
        {
            if (parameter is string pageKey)
            {
                foreach (var item in MenuItems)
                {
                    item.IsSelected = false;
                }

                var selectedItem = MenuItems.FirstOrDefault(x => x.PageKey == pageKey);
                if (selectedItem != null)
                {
                    selectedItem.IsSelected = true;
                }

                SelectedPage = pageKey;
            }
        }

        private void ToggleMenu()
        {
            IsMenuExpanded = !IsMenuExpanded;
            // Save settings change
            GlobalSettings.Instance.IsMenuExpanded = _isMenuExpanded;
        }

        private void ProcessProfilesPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProcessProfilesPageViewModel.CurrentActiveProfile) ||
                e.PropertyName == nameof(ProcessProfilesPageViewModel.CurrentEditingProfile))
            {
                UpdateProcessProfilesBadge();
            }
        }

        private void UpdateProcessProfilesBadge()
        {
            var processProfilesItem = MenuItems.FirstOrDefault(x => x.PageKey == "ProcessProfiles");
            if (processProfilesItem != null)
            {
                var hasUnsavedChanges = ProcessProfilesPageViewModel.CurrentActiveProfile != ProcessProfilesPageViewModel.CurrentEditingProfile;
                processProfilesItem.HasUnsavedChanges = hasUnsavedChanges;
            }
        }

        private void UpdateThemeColors()
        {
            var app = Application.Current;
            if (app == null) return;

            // Use FluentTheme's built-in theme variants
            if (IsDarkTheme)
            {
                app.RequestedThemeVariant = ThemeVariant.Dark;
                
                // Override with custom colors for our UI
                app.Resources["WindowBackgroundColor"] = Color.Parse("#202020");
                app.Resources["SidebarBackgroundColor"] = Color.Parse("#2B2B2B");
                app.Resources["CardBackgroundColor"] = Color.Parse("#2D2D2D");
                app.Resources["BorderColor"] = Color.Parse("#3F3F3F");
                app.Resources["PrimaryTextColor"] = Color.Parse("#FFFFFF");
                app.Resources["SecondaryTextColor"] = Color.Parse("#B0B0B0");
                app.Resources["HoverBackgroundColor"] = Color.Parse("#454545");
                app.Resources["SelectedBackgroundColor"] = Color.Parse("#505050");
                app.Resources["AccentButtonBackground"] = Color.Parse("#60CDFF");
                
                app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.Parse("#202020"));
                app.Resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.Parse("#2B2B2B"));
                app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.Parse("#2D2D2D"));
                app.Resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#3F3F3F"));
                app.Resources["PrimaryTextBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
                app.Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.Parse("#B0B0B0"));
                app.Resources["HoverBackgroundBrush"] = new SolidColorBrush(Color.Parse("#454545"));
                app.Resources["SelectedBackgroundBrush"] = new SolidColorBrush(Color.Parse("#505050"));
                app.Resources["AccentButtonBackgroundBrush"] = new SolidColorBrush(Color.Parse("#60CDFF"));
            }
            else
            {
                app.RequestedThemeVariant = ThemeVariant.Light;
                
                // Override with custom colors for our UI
                app.Resources["WindowBackgroundColor"] = Color.Parse("#F3F3F3");
                app.Resources["SidebarBackgroundColor"] = Color.Parse("#FAF9F8");
                app.Resources["CardBackgroundColor"] = Color.Parse("#FFFFFF");
                app.Resources["BorderColor"] = Color.Parse("#E0E0E0");
                app.Resources["PrimaryTextColor"] = Color.Parse("#202020");
                app.Resources["SecondaryTextColor"] = Color.Parse("#606060");
                app.Resources["HoverBackgroundColor"] = Color.Parse("#E8E8E8");
                app.Resources["SelectedBackgroundColor"] = Color.Parse("#D6D6D6");
                app.Resources["AccentButtonBackground"] = Color.Parse("#0078D4");
                
                app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.Parse("#F3F3F3"));
                app.Resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.Parse("#FAF9F8"));
                app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.Parse("#FFFFFF"));
                app.Resources["BorderBrush"] = new SolidColorBrush(Color.Parse("#E0E0E0"));
                app.Resources["PrimaryTextBrush"] = new SolidColorBrush(Color.Parse("#202020"));
                app.Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.Parse("#606060"));
                app.Resources["HoverBackgroundBrush"] = new SolidColorBrush(Color.Parse("#E8E8E8"));
                app.Resources["SelectedBackgroundBrush"] = new SolidColorBrush(Color.Parse("#D6D6D6"));
                app.Resources["AccentButtonBackgroundBrush"] = new SolidColorBrush(Color.Parse("#0078D4"));
            }
        }

        /// <summary>
        /// Apply settings from GlobalSettings to this ViewModel
        /// </summary>
        private void ApplyGlobalSettings()
        {
            var settings = GlobalSettings.Instance;
            
            // Apply menu expansion state
            if (_isMenuExpanded != settings.IsMenuExpanded)
            {
                _isMenuExpanded = settings.IsMenuExpanded;
                OnPropertyChanged(nameof(IsMenuExpanded));
                OnPropertyChanged(nameof(MenuWidth));
            }
            
            // Apply theme
            if (_isDarkTheme != settings.IsDarkTheme)
            {
                _isDarkTheme = settings.IsDarkTheme;
                OnPropertyChanged(nameof(IsDarkTheme));
                UpdateThemeColors();
            }
            
            // Apply language
            var expectedLanguageIndex = settings.CurrentLanguage == "en" ? 0 : 1;
            if (_selectedLanguageIndex != expectedLanguageIndex)
            {
                _selectedLanguageIndex = expectedLanguageIndex;
                OnPropertyChanged(nameof(SelectedLanguageIndex));
                _localization.CurrentLanguage = settings.CurrentLanguage;
            }
        }

        /// <summary>
        /// Called when global settings have changed
        /// </summary>
        public void OnSettingsChanged()
        {
            ApplyGlobalSettings();
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