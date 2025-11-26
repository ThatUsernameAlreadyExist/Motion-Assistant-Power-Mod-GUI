using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows11Settings.Managers;
using Windows11Settings.Models;

namespace Windows11Settings.Resources.Localization
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager _instance;
        private string _currentLanguage = "en";
        private Dictionary<string, string> _strings = new Dictionary<string, string>();
        private Dictionary<string, string> _availableLanguages = new Dictionary<string, string>();
        private readonly string _localizationFolderPath;

        public static LocalizationManager Instance => _instance ?? (_instance = new LocalizationManager());

        private LocalizationManager()
        {
            // Get the path to the Localization folder relative to the executable
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _localizationFolderPath = Path.Combine(baseDirectory, "Localization");
            
            // Ensure the Localization folder exists
            if (!Directory.Exists(_localizationFolderPath))
            {
                // For debug.
                string relativePath = Path.Combine(baseDirectory, "..", "..", "..", "..", "Resources", "Localization");
                _localizationFolderPath = Path.GetFullPath(relativePath);
            }
            
            LoadAvailableLanguages();
            LoadLanguage(GlobalSettings.Instance.CurrentLanguage);
        }

        private void LoadAvailableLanguages()
        {
            _availableLanguages.Clear();
            
            // Scan for .ini files in the Localization folder
            if (Directory.Exists(_localizationFolderPath))
            {
                var iniFiles = Directory.GetFiles(_localizationFolderPath, "*.ini");
                foreach (var iniFile in iniFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(iniFile);
                        var iniManager = new IniFileManager(iniFile);
                        
                        // Get the language name from the General section
                        var languageName = iniManager.GetValue("General", "LanguageName", fileName);
                        _availableLanguages[fileName] = languageName;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading language file {iniFile}: {ex.Message}");
                    }
                }
            }
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    bool lastCmdState = GlobalAppManager.Instance.IsCmdSendingEnabled;
                    GlobalAppManager.Instance.IsCmdSendingEnabled = false;

                    LoadLanguage(value);
                    
                    // Raise multiple PropertyChanged notifications to ensure Avalonia picks up the change
                    OnPropertyChanged();  // CurrentLanguage itself
                    
                    // Force complete refresh - raise these immediately after language loads
                    OnLanguageChanged();

                    GlobalAppManager.Instance.IsCmdSendingEnabled = lastCmdState;
                }
            }
        }

        public Dictionary<string, string> AvailableLanguages => _availableLanguages;

        public string this[string key]
        {
            get => _strings.TryGetValue(key, out var value) ? value : key;
        }

        private void LoadLanguage(string language)
        {
            _strings.Clear();
            _currentLanguage = language;

            string iniFilePath = Path.Combine(_localizationFolderPath, $"{language}.ini");
            
            if (File.Exists(iniFilePath))
            {
                try
                {
                    var iniManager = new IniFileManager(iniFilePath);
                    LoadTranslationsFromIni(iniManager);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading language file {language}: {ex.Message}");
                }
            }
            else if (language != "en")
            {
                LoadLanguage("en");
            }
        }

        private void LoadTranslationsFromIni(IniFileManager iniManager)
        {
            // Load all sections and their key-value pairs
            var sections = iniManager.GetSectionNames();
            foreach (var section in sections)
            {
                var keys = iniManager.GetKeyNames(section);
                foreach (var key in keys)
                {
                    var value = iniManager.GetValue(section, key, string.Empty);
                    _strings[key] = value;
                }
            }
        }

        private void OnLanguageChanged()
        {
            // Notify using multiple patterns to ensure Avalonia picks up the change
            // Pattern 1: Indexer notation for Avalonia
            OnPropertyChanged("Item[]");
            // Pattern 2: All properties changed
            OnPropertyChanged(string.Empty);
            // Pattern 3: Explicit null (alternative to string.Empty)
            OnPropertyChanged(null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
