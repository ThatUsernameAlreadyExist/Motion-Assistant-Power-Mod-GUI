using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Windows11Settings.Managers;

namespace Windows11Settings.Models
{
    /// <summary>
    /// Global settings manager that handles loading and saving settings from/to pmgui.ini
    /// </summary>
    public class GlobalSettings : INotifyPropertyChanged
    {
        private static GlobalSettings _instance;
        private readonly string _settingsFilePath;
        private readonly IniFileManager _iniFile;
        
        // Settings properties
        private bool _isMenuExpanded = true;
        private bool _isDarkTheme = false;
        private string _currentLanguage = "en";

        public static GlobalSettings Instance => _instance ?? (_instance = new GlobalSettings());

        private GlobalSettings()
        {
            // Get the path to the executable directory
            var executablePath = AppDomain.CurrentDomain.BaseDirectory;
            _settingsFilePath = Path.Combine(executablePath, "pmgui.ini");
            _iniFile = new IniFileManager(_settingsFilePath);
            
            LoadSettings();
        }

        #region Settings Properties

        public bool IsMenuExpanded
        {
            get => _isMenuExpanded;
            set
            {
                if (SetProperty(ref _isMenuExpanded, value))
                {
                    SaveSettings();
                    NotifyMainWindowSettingsChanged();
                }
            }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    SaveSettings();
                    NotifyMainWindowSettingsChanged();
                    NotifyAdvancedPageSettingsChanged();
                }
            }
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (SetProperty(ref _currentLanguage, value))
                {
                    SaveSettings();
                    NotifyMainWindowSettingsChanged();
                    NotifyAdvancedPageSettingsChanged();
                }
            }
        }

        #endregion

        #region Settings Loading and Saving

        /// <summary>
        /// Load settings from pmgui.ini file
        /// </summary>
        public void LoadSettings()
        {
            try
            {

                bool systemIsDarkTheme = false;
                try
                {
                    // Check Windows system theme using registry (Windows 10/11)
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (key != null)
                        {
                            var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                            systemIsDarkTheme = appsUseLightTheme != null && (int)appsUseLightTheme == 0;
                        }
                    }
                }
                catch
                {
                    // Fallback to false if registry access fails
                    systemIsDarkTheme = false;
                }

                // Get system language
                string systemLanguage = "en";
                try
                {
                    var culture = System.Globalization.CultureInfo.CurrentCulture;
                    var langName = culture.TwoLetterISOLanguageName.ToLower();

                    // Validate that it's a proper two-letter code
                    if (langName.Length == 2 && char.IsLetter(langName[0]) && char.IsLetter(langName[1]))
                    {
                        systemLanguage = langName;
                    }
                }
                catch
                {
                    // Fallback to English if culture access fails
                    systemLanguage = "en";
                }

                // Load UI settings
                _isMenuExpanded = _iniFile.GetValue("UI", "IsMenuExpanded", true);
                _isDarkTheme = _iniFile.GetValue("UI", "IsDarkTheme", systemIsDarkTheme);
                _currentLanguage = _iniFile.GetValue("UI", "CurrentLanguage", systemLanguage);

                OnPropertyChanged(nameof(IsMenuExpanded));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(CurrentLanguage));

                // Apply settings to ViewModels
                ApplySettingsToViewModels();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Save current settings to pmgui.ini file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Save UI settings
                _iniFile.SetValue("UI", "IsMenuExpanded", _isMenuExpanded.ToString());
                _iniFile.SetValue("UI", "IsDarkTheme", _isDarkTheme.ToString());
                _iniFile.SetValue("UI", "CurrentLanguage", _currentLanguage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply loaded settings to the appropriate ViewModels
        /// </summary>
        private void ApplySettingsToViewModels()
        {
            try
            {
                // Find the main window view model via GlobalAppManager
                var mainVM = GlobalAppManager.Instance.MainViewModel;
                if (mainVM != null)
                {
                    // Apply menu expansion state
                    mainVM.OnSettingsChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying settings to ViewModels: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            _isMenuExpanded = true;
            _isDarkTheme = false;
            _currentLanguage = "en";

            OnPropertyChanged(nameof(IsMenuExpanded));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(CurrentLanguage));

            SaveSettings();
            ApplySettingsToViewModels();
        }

        #endregion

        #region Event Notification

        /// <summary>
        /// Notify MainWindowViewModel that settings have changed
        /// </summary>
        private void NotifyMainWindowSettingsChanged()
        {
            try
            {
                // This will be called when settings change, allowing MainWindowViewModel to react
                var mainVM = GlobalAppManager.Instance.MainViewModel;
                if (mainVM != null)
                {
                    mainVM.OnSettingsChanged();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying MainWindow: {ex.Message}");
            }
        }

        /// <summary>
        /// Notify AdvancedPageViewModel that settings have changed
        /// </summary>
        private void NotifyAdvancedPageSettingsChanged()
        {
            try
            {
                // AdvancedPageViewModel will listen for these changes via the GlobalAppManager
                GlobalAppManager.Instance?.OnSettingsChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying AdvancedPage: {ex.Message}");
            }
        }

        #endregion

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