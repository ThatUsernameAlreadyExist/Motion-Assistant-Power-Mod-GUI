using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using PmGui.Managers;

namespace PmGui.Models
{
    /// <summary>
    /// Global settings manager that handles loading and saving settings from/to pmgui.ini
    /// Automatically selects the appropriate settings file location based on write access permissions:
    /// - Uses pmgui.ini near executable if write access is available
    /// - Falls back to %LocalAppData%\pmgui\pmgui.ini if no write access to executable directory
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
        private bool _useGamepad = true;
        private bool _useHwRendering = true;
        
        // Page visibility settings - stores pages that should be hidden
        // All pages are visible by default, only hidden pages are stored in this list
        private HashSet<string> _hiddenPages = new HashSet<string>();

        public static GlobalSettings Instance => _instance ?? (_instance = new GlobalSettings(true));

        public GlobalSettings(bool withApply)
        {
            // Determine the appropriate settings file path based on write access
            _settingsFilePath = DetermineSettingsFilePath();
            _iniFile = new IniFileManager(_settingsFilePath);
            
            LoadSettings(withApply);
        }

        /// <summary>
        /// Determines the appropriate settings file path based on write access permissions
        /// </summary>
        /// <returns>Path to the settings file</returns>
        private string DetermineSettingsFilePath()
        {
            try
            {
                // Get the path near the executable
                var executablePath = AppDomain.CurrentDomain.BaseDirectory;
                var iniFileNearExecutable = Path.Combine(executablePath, "pmgui.ini");

                // Check if we have write access to the executable directory
                if (HasWriteAccessToDirectory(executablePath))
                {
                    return iniFileNearExecutable;
                }
            }
            catch
            {
            }

            // Fallback to local app data
            var localAppDataPath = GetLocalAppDataSettingsPath();
            return localAppDataPath;
        }

        /// <summary>
        /// Checks if the current process has write access to the specified directory
        /// </summary>
        /// <param name="directoryPath">Directory path to check</param>
        /// <returns>True if write access is available, false otherwise</returns>
        private bool HasWriteAccessToDirectory(string directoryPath)
        {
            try
            {
                // Check if directory exists
                if (!Directory.Exists(directoryPath))
                {
                    return false;
                }

                // Try to get directory security to check permissions
                var directoryInfo = new DirectoryInfo(directoryPath);
                var directorySecurity = directoryInfo.GetAccessControl();

                // Get current user identity
                var currentUser = System.Security.Principal.WindowsIdentity.GetCurrent();
                var currentUserSid = currentUser.User;

                // Check if current user has write permission
                var rules = directorySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule.IdentityReference.Equals(currentUserSid) || 
                        currentUser.Groups.Contains(rule.IdentityReference))
                    {
                        if ((rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData ||
                            (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify ||
                            (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                return true;
                            }
                        }
                    }
                }

                // If we can't determine permissions through security rules, try a safer approach
                // Try to create a temporary file to test write access
                return TestWriteAccess(directoryPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests write access by attempting to create a temporary file
        /// </summary>
        /// <param name="directoryPath">Directory path to test</param>
        /// <returns>True if write access is available, false otherwise</returns>
        private bool TestWriteAccess(string directoryPath)
        {
            try
            {
                var testFileName = $"pmgui_folder_rights_tst_{Guid.NewGuid().ToString("N").Substring(0, 8)}.tmp";
                var testFilePath = Path.Combine(directoryPath, testFileName);
                
                // Try to create and write to a test file
                using (var fs = File.Create(testFilePath, 1, FileOptions.DeleteOnClose))
                {
                    var testBytes = new byte[] { 0 };
                    fs.Write(testBytes, 0, 1);
                }
                
                // If we get here, we have write access
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                // No write access
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the settings file path in local application data
        /// </summary>
        /// <returns>Path to settings file in local app data</returns>
        private string GetLocalAppDataSettingsPath()
        {
            try
            {
                // Get the local application data folder
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                // Create the pmgui subfolder if it doesn't exist
                var pmguiFolder = Path.Combine(localAppData, "pmgui");
                
                if (!Directory.Exists(pmguiFolder))
                {
                    Directory.CreateDirectory(pmguiFolder);
                }

                return Path.Combine(pmguiFolder, "pmgui.ini");
            }
            catch
            {
                // Final fallback - try to use current directory
                var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pmgui.ini");
                return fallbackPath;
            }
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
                }
            }
        }

        public bool UseGamepad
        {
            get => _useGamepad;
            set
            {
                if (SetProperty(ref _useGamepad, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool UseHwRendering
        {
            get => _useHwRendering;
            set
            {
                if (SetProperty(ref _useHwRendering, value))
                {
                    SaveSettings();
                }
            }
        }

        #region Page Visibility Methods

        /// <summary>
        /// Check if a page is visible
        /// </summary>
        /// <param name="pageName">The name of the page</param>
        /// <returns>True if the page should be visible, false if hidden</returns>
        public bool IsPageVisible(string pageName)
        {
            // Page is visible if it's not in the hidden pages list
            return !_hiddenPages.Contains(pageName);
        }

        /// <summary>
        /// Set the visibility of a page
        /// </summary>
        /// <param name="pageName">The name of the page</param>
        /// <param name="isVisible">True to show the page, false to hide it</param>
        public void SetPageVisibility(string pageName, bool isVisible)
        {
            bool wasVisible = IsPageVisible(pageName);
            
            if (isVisible)
            {
                // Remove from hidden pages if it exists
                _hiddenPages.Remove(pageName);
            }
            else
            {
                // Add to hidden pages
                _hiddenPages.Add(pageName);
            }

            // Only save and notify if the visibility actually changed
            if (wasVisible != isVisible)
            {
                SaveSettings();
            }
        }

        #endregion

        #endregion

        #region Settings Loading and Saving

        /// <summary>
        /// Load settings from pmgui.ini file
        /// </summary>
        public void LoadSettings(bool withApply)
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
                _useGamepad = _iniFile.GetValue("UI", "UseGamepad", true);
                _useHwRendering = _iniFile.GetValue("UI", "UseHwRendering", true);

                // Load page visibility settings - stored as comma-separated list of hidden pages
                string hiddenPagesString = _iniFile.GetValue("UI", "HiddenPages", "");
                _hiddenPages.Clear();

                if (!string.IsNullOrEmpty(hiddenPagesString))
                {
                    var hiddenPages = hiddenPagesString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                    
                    foreach (var page in hiddenPages)
                    {
                        _hiddenPages.Add(page);
                    }
                }

                if (withApply)
                {
                    OnPropertyChanged(nameof(IsMenuExpanded));
                    OnPropertyChanged(nameof(IsDarkTheme));
                    OnPropertyChanged(nameof(CurrentLanguage));
                    OnPropertyChanged(nameof(UseGamepad));

                    // Apply settings to ViewModels
                    ApplySettingsToViewModels();
                }
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
                _iniFile.SetValue("UI", "UseGamepad", _useGamepad.ToString());
                _iniFile.SetValue("UI", "UseHwRendering", _useHwRendering.ToString());

                // Save page visibility settings as comma-separated list of hidden pages
                string hiddenPagesString = string.Join(",", _hiddenPages);
                _iniFile.SetValue("UI", "HiddenPages", hiddenPagesString);
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