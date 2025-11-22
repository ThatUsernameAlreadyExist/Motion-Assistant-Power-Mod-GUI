using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public static LocalizationManager Instance => _instance ?? (_instance = new LocalizationManager());

        private LocalizationManager()
        {
            LoadLanguage(GlobalSettings.Instance.CurrentLanguage);
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

        public string this[string key]
        {
            get => _strings.TryGetValue(key, out var value) ? value : key;
        }

        private void LoadLanguage(string language)
        {
            _strings.Clear();

            _currentLanguage = language;

            switch (language)
            {
                case "ru":
                    LoadRussian();
                    break;
                default:
                    LoadEnglish();
                    break;
            }
        }

        // Resources/Localization/LocalizationManager.cs
// ... existing code ...

private void LoadEnglish()
{
    _strings = new Dictionary<string, string>
    {
        // Window
        ["Settings"] = "Power Mod",
    
        // Theme
        ["ToggleNavigation"] = "Toggle Navigation",
    
        // Menu Items
        ["Monitoring"] = "Monitoring",
        ["CPU"] = "CPU",
        ["GPU"] = "GPU",
        ["Fan"] = "Fan",
        ["Gyroscope"] = "Gyroscope",
        ["OSDOverlay"] = "Overlay",
        ["ProcessProfiles"] = "Profiles",
        ["Advanced"] = "Advanced",
    
        // Monitoring Page
        ["MonitoringTitle"] = "System Monitoring",
        ["TDPLimit"] = "TDP Limit, W",
        ["GPULock"] = "Optimized GPU Frequency, MHz",
        ["CPUBoost"] = "Turbo CPU",
        ["PackagePower"] = "CPU Power",
        ["CPUTemperature"] = "CPU Temperature",
        ["CPUUsage"] = "CPU Usage",
        ["GPUUsage"] = "GPU Usage",
        ["FanSpeed"] = "Fan Speed",
        ["BatteryChargeRate"] = "Battery Power",
        ["On"] = "On",
        ["Off"] = "Off",
        ["Watt"] = "W",
    
        // CPU Page
        ["FromPowerLine"] = "From Power Line",
        ["FromBattery"] = "From Battery",
        ["TDP"] = "TDP",
        ["TDPDescription"] = "CPU power limit, W",
        ["CpuFrequencyLimit"] = "CPU clock limit",
        ["CpuFrequencyLimitDescription"] = "Maximum CPU clock frequency, MHz",
        ["FpsLimit"] = "FPS limit",
        ["FpsLimitDescription"] = "Maximum frame rate",
        ["CpuCoresLimit"] = "Active CPU cores",
        ["CpuCoresLimitDescription"] = "Processor cores limitation",
        ["CPUBoost"] = "CPU Boost",
        ["CPUBoostDescription"] = "Use CPU Turbo Boost",
        ["AutoOptimizeCPUFrequency"] = "Automatic CPU clock optimization",
        ["AutoOptimizeCPUFrequencyDescription"] = "Clock frequency limit based on selected TDP",
        ["UniteBatteryAndPowerlineCPUPresets"] = "Unify TDP limits",
        ["UniteBatteryAndPowerlineCPUPresetsDescription"] = "Unified TDP limit from Power Line/Battery",
        ["UniteBatteryAndPowerlineFPSLimit"] = "Unify FPS limits",
        ["UniteBatteryAndPowerlineFPSLimitDescription"] = "Unified FPS limit from Power Line/Battery",
        ["LoadPresetAtStart"] = "Auto apply TDP at profile load",
        ["LoadPresetAtStartDescription"] = "Apply TDP limits on application start/profile change",
        ["Max"] = "Maximum",
        ["None"] = "None",
        ["All"] = "All",
        ["OnlyBig"] = "Only P-Cores",
        ["OnlySmall"] = "Only E-Cores",
        ["PriorityBig"] = "Prefer P-Cores",
        ["PrioritySmall"] = "Prefer E-Cores",
        ["Active"] = "Active",
        ["CPUPerformanceSettings"] = "CPU Settings",


        // GPU Page
        ["ClockSpeed"] = "Clock speed",
        ["OptimizeGpuClocks"] = "Optimize GPU clocks",
        ["OptimizeGpuClocksDescription"] = "Adjustment using TDP limits and GPU load",
        ["OptimizationMode"] = "Policy",
        ["FixedClock"] = "Stable",
        ["AdaptiveClock"] = "Float",
        ["CustomGpuClocksRange"] = "Custom GPU clock range",
        ["CustomGpuClocksRangeDescription"] = "Sets the range for GPU clock optimization",
        ["MinGpuClock"] = "Minimum clock",
        ["MinGpuClockDescription"] = "Used at 0% load, MHz",
        ["MaxGpuClock"] = "Maximum clock",
        ["MaxGpuClockDescription"] = "Used at > 90% load, MHz",
        ["Apply"] = "Apply",
        ["ResetGpu"] = "Reset GPU",
        ["ResetGpuDescription"] = "Reset all GPU clock limits via entering sleep mode",
        ["GPUPerformanceSettings"] = "GPU Settings",
        ["ResetGpuTip"] = "Attention: After enabling optimization, the GPU frequency will be completely controlled by this program. To return control to the system driver (automatic GPU frequency), you must reset the GPU (simply disabling optimization is not enough).",

        // Fan Page
        ["FanControl"] = "Fan Control",
        ["EnableFanSpeedControl"] = "Enable fan speed control",
        ["EnableFanSpeedControlDescription"] = "Manual fan speed adjustment",
        ["FanSpeedPreset"] = "Current profile",
        ["FanSpeedPresetDescription"] = "System cooling configuration",
        ["FanSpeedControlType"] = "Control type",
        ["FanSpeedControlTypeDescription"] = "Fixed or variable speed",
        ["Quiet"] = "Quiet",
        ["Balanced"] = "Balanced",
        ["Performance"] = "Performance",
        ["Custom"] = "Custom",
        ["FixedSpeed"] = "Fixed",
        ["SpeedCurve"] = "Curve",
        ["FanSpeed"] = "Fan speed",
        ["FanSpeedDescription"] = "Active regardless of temperature",
        ["SpeedCurveEditor"] = "Curve editor",
        ["SpeedCurveEditorDescription"] = "Adjust fan speed based on temperature",
        ["Temperature45"] = "45°C",
        ["Temperature60"] = "60°C",
        ["Temperature70"] = "70°C",  
        ["Temperature80"] = "80°C",
        ["DelayTimeout"] = "Delay",
        ["DelayTimeoutDescription"] = "Time to wait (for smooth speed change), s",
        ["Apply"] = "Apply",
    
        // Gyroscope Page
        ["GyroscopeInfo"] = "Gyroscope",
    
    
        // Advanced Page
        ["AdvancedSettings"] = "Advanced Settings",
        ["AppearanceSettings"] = "Appearance",
        ["Theme"] = "Dark theme",
        ["Language"] = "Language",
        ["UseNewInterface"] = "Use new interface",
        ["UseNewInterfaceHint"] = "Disable to open the classic Motion Assistant",
        ["AddToSystemAutorun"] = "Autorun",
        ["AddToSystemAutorunHint"] = "Start Motion Assistant on OS boot",
        ["DisableSystemMonitoring"] = "Disable system monitoring",
        ["DisableSystemMonitoringHint"] = "Also disables fan control and GPU optimization",
        ["DisableBluetoothInSleepMode"] = "Disable bluetooth in sleep mode",
        ["DisableBluetoothDescription"] = "Saves energy in Modern Standby mode",
        ["WindowSizeAtStartup"] = "Default window state",
        ["WindowSizeNormal"] = "Normal",
        ["WindowSizeMinimized"] = "Minimized",
        ["WindowSizeMaximized"] = "Maximized",
        ["MinimizeToSystemTray"] = "Minimize to tray",
        ["CheckForUpdates"] = "Check for updates",
        ["CheckForUpdatesDescription"] = "Open the GitHub page for manual update (replace files manually)",
        ["About"] = "About",
    
        // Language
        ["English"] = "English",
        ["Russian"] = "Russian",
    
        // Gyroscope Page
        ["GyroscopeSettings"] = "Gyroscope Settings",
        ["EnableGyroscope"] = "Enable gyroscope",
        ["EnableGyroscopeDescription"] = "Cursor/Aiming control using gyroscope",
        ["HighPrecisionGyroscope"] = "High speed gyroscope",
        ["HighPrecisionGyroscopeDescription"] = "Note: This option may increase CPU load",
        ["DisableBoschAccelerometer"] = "Disable Bosch Driver (BMI160) when inactive",
        ["DisableBoschAccelerometerDescription"] = "Reduces power consumption and CPU load",
        ["GyroscopeActivationButton"] = "Gyroscope activation button",
        ["GyroscopeActivationButtonTip"] = "Active only while the button is pressed",
        ["NotSet"] = "Not set",
        ["LeftTrigger"] = "Left trigger",
        ["RightTrigger"] = "Right trigger",
        ["LeftOrRightTrigger"] = "Left or Right trigger",

        // OSD Overlay Page
        ["OSDOverlaySettings"] = "OSD Overlay Settings",
        ["EnableOSDOverlay"] = "Enable OSD",
        ["EnableOSDOverlayDescription"] = "RivaTuner Statistics Server must be running in the background",
        ["OSDType"] = "OSD layout",
        ["OSDTypeDescription"] = "Type of information in games",
        ["OSDTypeFull"] = "Full",
        ["OSDTypeOneLine"] = "One Line",
        ["OSDTypeSimple"] = "Simple",
        ["DownloadRTSSDescription"] = "Download the latest version of RivaTuner Statistics Server for OSD overlay functionality",
        ["Download"] = "Download RTSS",
    
        // Process Profiles Page
        ["ProcessProfilesSettings"] = "Profiles Settings",
        ["KeepLastProcessProfile"] = "Lock process profile",
        ["KeepLastProcessProfileDescription"] = "The profile remains active after the process ends",
        ["ActiveProfile"] = "Active profile",
        ["CurrentEditingProfile"] = "Current editing profile",
        ["ProfilesList"] = "General profiles",
        ["ProcessList"] = "Individual process profiles",
        ["Default"] = "Default",
        ["Remove"] = "Remove",
        ["Reset"] = "Reset",
        ["Add"] = "Add",
        ["Help"] = "Help",
        ["Edit"] = "Edit",
        ["TdpLimitsAtStartNote"] = "Note: Enable 'Auto apply TDP at profile load' in the CPU settings to automatically apply the TDP limit when changing or applying a profile.",
        ["ProcessProfilesHelp1"] = "General profiles help quickly change settings (TDP, FPS limit, etc).",
        ["ProcessProfilesHelp2"] = "Select a profile or create a new one, press 'Apply' - settings will be loaded.",
        ["ProcessProfilesHelp3"] = "Settings of all tabs are automatically saved in the selected profile when changed.",
        ["ProcessProfilesHelp4"] = "Also you can set settings only for a specific process (game).",
        ["ProcessProfilesHelp5"] = "Add a process or select it, click 'Edit', and adjust the parameters in the relevant tabs.",
        ["ProcessProfilesHelp6"] = "When the window of this process is activated, settings from its profile are automatically applied.",
        ["ProcessProfilesHelp7"] = "When the window of a process is closed or minimized, the general profile is activated (if not selected 'Lock Process Profile' option).",
        // Keep the old key for compatibility

        // Dialog
        ["AddProfile"] = "Add profile",
        ["EnterProfileName"] = "Enter profile name:",
        ["ProfileName"] = "Profile name",
        ["OK"] = "OK",
        ["Cancel"] = "Cancel"
    };

}

private void LoadRussian()
{
    _strings = new Dictionary<string, string>
    {
        // Window
        ["Settings"] = "Power Mod",
        
        // Theme
        ["ToggleNavigation"] = "Переключить навигацию",
        
        // Menu Items
        ["Monitoring"] = "Мониторинг",
        ["CPU"] = "Процессор",
        ["GPU"] = "Видеокарта",
        ["Fan"] = "Вентиляторы",
        ["Gyroscope"] = "Гироскоп",
        ["OSDOverlay"] = "Оверлей",
        ["ProcessProfiles"] = "Профили",
        ["Advanced"] = "Дополнительно",
        
        // Monitoring Page
        ["MonitoringTitle"] = "Мониторинг системы",
        ["TDPLimit"] = "Лимит TDP, Вт",
        ["GPULock"] = "Оптимизированная частота GPU, МГц",
        ["CPUBoost"] = "Турбо CPU",
        ["PackagePower"] = "Потребление CPU",
        ["CPUTemperature"] = "Температура CPU",
        ["CPUUsage"] = "Нагрузка CPU",
        ["GPUUsage"] = "Нагрузка GPU",
        ["FanSpeed"] = "Скорость вентилятора",
        ["BatteryChargeRate"] = "Потребление батареи",
        ["On"] = "Вкл",
        ["Off"] = "Выкл",
        ["Watt"] = "Вт",
        
        // CPU Page
        ["FromPowerLine"] = "От сети",
        ["FromBattery"] = "От батареи",
        ["TDP"] = "TDP",
        ["TDPDescription"] = "Лимит энергопотребления процессора, Вт",
        ["CpuFrequencyLimit"] = "Лимит частоты CPU",
        ["CpuFrequencyLimitDescription"] = "Частота процессора, МГц",
        ["FpsLimit"] = "Лимит FPS",
        ["FpsLimitDescription"] = "Максимальная частота кадров",
        ["CpuCoresLimit"] = "Активные ядра CPU",
        ["CpuCoresLimitDescription"] = "Ограничение активных ядер",
        ["CPUBoost"] = "Турбо CPU",
        ["CPUBoostDescription"] = "Использовать Turbo Boost процессора",
        ["AutoOptimizeCPUFrequency"] = "Автоматическая оптимизация частоты CPU",
        ["AutoOptimizeCPUFrequencyDescription"] = "Лимит частоты в зависимости от выбранного TDP",
        ["UniteBatteryAndPowerlineCPUPresets"] = "Объединить лимит TDP",
        ["UniteBatteryAndPowerlineCPUPresetsDescription"] = "Общий лимит TDP от сети/батареи",
        ["UniteBatteryAndPowerlineFPSLimit"] = "Объединить лимит FPS",
        ["UniteBatteryAndPowerlineFPSLimitDescription"] = "Общий лимит FPS от сети/батареи",
        ["LoadPresetAtStart"] = "Применять лимит TDP при загрузке профиля",
        ["LoadPresetAtStartDescription"] = "Автоматически применять настройки TDP при запуске/смене профиля",
        ["Max"] = "Максимальная",
        ["None"] = "Нет",
        ["All"] = "Все",
        ["OnlyBig"] = "Только большие",
        ["OnlySmall"] = "Только малые",
        ["PriorityBig"] = "Приоритет больших",
        ["PrioritySmall"] = "Приоритет малых",
        ["Active"] = "Активно",
        ["CPUPerformanceSettings"] = "Настройки процессора",


        // GPU Page
        ["ClockSpeed"] = "Частота",
        ["OptimizeGpuClocks"] = "Оптимизировать частоту GPU",
        ["OptimizeGpuClocksDescription"] = "На основе лимита TDP и нагрузки на GPU",
        ["OptimizationMode"] = "Режим оптимизации",
        ["FixedClock"] = "Фиксированная частота",
        ["AdaptiveClock"] = "Адаптивная частота",
        ["CustomGpuClocksRange"] = "Свой диапазон частот",
        ["CustomGpuClocksRangeDescription"] = "Задает границы оптимизации частот GPU",
        ["MinGpuClock"] = "Минимальная частота",
        ["MinGpuClockDescription"] = "Используется при нагрузке 0%, МГц",
        ["MaxGpuClock"] = "Максимальная частота",
        ["MaxGpuClockDescription"] = "Используется при нагрузке > 90%, МГц",
        ["Apply"] = "Применить",
        ["ResetGpu"] = "Сброс GPU",
        ["ResetGpuDescription"] = "Сбросить все ограничения частот GPU через переход в спящий режим",
        ["GPUPerformanceSettings"] = "Настройки видеокарты",
        ["ResetGpuTip"] = "Внимание: после включения оптимизации частота GPU будет полностью управляться данной программой. Для возврата управления системному драйверу (автоматическая частота GPU) необходимо выполнить сброс GPU, простого выключения оптимизации недостаточно.",

        // Fan Page
        ["FanControl"] = "Управление вентиляторами",
        ["EnableFanSpeedControl"] = "Управление скоростью",
        ["EnableFanSpeedControlDescription"] = "Ручная регулировка скорости вентилятора",
        ["FanSpeedPreset"] = "Текущий профиль",
        ["FanSpeedPresetDescription"] = "Конфигурация охлаждения системы",
        ["FanSpeedControlType"] = "Режим управления",
        ["FanSpeedControlTypeDescription"] = "Постоянная или переменная скорость",
        ["Quiet"] = "Тихий",
        ["Balanced"] = "Сбалансированный",
        ["Performance"] = "Производительный",
        ["Custom"] = "Пользовательский",
        ["FixedSpeed"] = "Постоянная скорость",
        ["SpeedCurve"] = "Переменная скорость",
        ["FanSpeed"] = "Скорость вентилятора",
        ["FanSpeedDescription"] = "Активна вне зависимости от температуры",
        ["SpeedCurveEditor"] = "Редактор переменной скорости",
        ["SpeedCurveEditorDescription"] = "Настройте скорость вентилятора в зависимости от температуры",
        ["Temperature45"] = "45°C",
        ["Temperature60"] = "60°C",
        ["Temperature70"] = "70°C",  
        ["Temperature80"] = "80°C",
        ["DelayTimeout"] = "Задержка",
        ["DelayTimeoutDescription"] = "Время ожидания для плавного изменения скорости, c",
        ["Apply"] = "Применить",
        
        // Gyroscope Page
        ["GyroscopeInfo"] = "Гироскоп",
        
        
        // Advanced Page
        ["AdvancedSettings"] = "Дополнительные настройки",
        ["AppearanceSettings"] = "Внешний вид",
        ["Theme"] = "Темная тема",
        ["Language"] = "Язык",
        ["UseNewInterface"] = "Использовать новый интерфейс",
        ["UseNewInterfaceHint"] = "Выключите для открытия классического Motion Assistant",
        ["AddToSystemAutorun"] = "Автозапуск",
        ["AddToSystemAutorunHint"] = "Запускать Motion Assistant при загрузке ОС",
        ["DisableSystemMonitoring"] = "Отключить мониторинг системы",
        ["DisableSystemMonitoringHint"] = "Также отключает управление вентилятором и оптимизацию GPU",
        ["DisableBluetoothInSleepMode"] = "Отключать Bluetooth в режиме сна",
        ["DisableBluetoothDescription"] = "Экономит энергию в режиме Modern Standby",
        ["WindowSizeAtStartup"] = "Окно при запуске",
        ["WindowSizeNormal"] = "Обычное",
        ["WindowSizeMinimized"] = "Свернутое",
        ["WindowSizeMaximized"] = "Развернутое",
        ["MinimizeToSystemTray"] = "Сворачивать в системный трей",
        ["CheckForUpdates"] = "Проверить обновления",
        ["CheckForUpdatesDescription"] = "Открыть страницу GitHub для ручного обновления (замените файлы вручную)",
        ["About"] = "О программе",
        
        // Language
        ["English"] = "English",
        ["Russian"] = "Русский",
        
        // Gyroscope Page
        ["GyroscopeSettings"] = "Настройки гироскопа",
        ["EnableGyroscope"] = "Использовать гироскоп",
        ["EnableGyroscopeDescription"] = "Управление курсором/прицелом с помощью гироскопа",
        ["HighPrecisionGyroscope"] = "Повышенная точность гироскопа",
        ["HighPrecisionGyroscopeDescription"] = "Внимание: эта опция может увеличить нагрузку на CPU",
        ["DisableBoschAccelerometer"] = "Отключать драйвер Bosch (BMI160) при неактивности",
        ["DisableBoschAccelerometerDescription"] = "Снижает энергопотребление и нагрузку на CPU",
        ["GyroscopeActivationButton"] = "Кнопка активации гироскопа",
        ["GyroscopeActivationButtonTip"] = "Управление только при зажатой кнопке",
        ["NotSet"] = "Не задано",
        ["LeftTrigger"] = "Левый триггер",
        ["RightTrigger"] = "Правый триггер",
        ["LeftOrRightTrigger"] = "Левый или Правый триггер",

        // OSD Overlay Page
        ["OSDOverlaySettings"] = "Настройки оверлея",
        ["EnableOSDOverlay"] = "Оверлей RTSS",
        ["EnableOSDOverlayDescription"] = "RivaTuner Statistics Server должен быть запущен в фоновом режиме",
        ["OSDType"] = "Тип оверлея",
        ["OSDTypeDescription"] = "Тип отображения информации в играх",
        ["OSDTypeFull"] = "Полный",
        ["OSDTypeOneLine"] = "Одна строка",
        ["OSDTypeSimple"] = "Простой",
        ["DownloadRTSSDescription"] = "Загрузить последнюю версию RivaTuner Statistics Server для работы оверлея",
        ["Download"] = "Скачать RTSS",
        
        // Process Profiles Page
        ["ProcessProfilesSettings"] = "Настройки профилей",
        ["KeepLastProcessProfile"] = "Сохранять последний профиль процесса",
        ["KeepLastProcessProfileDescription"] = "После завершения процесса его профиль останется активным",
        ["ActiveProfile"] = "Активный профиль",
        ["CurrentEditingProfile"] = "Текущий редактируемый профиль",
        ["ProfilesList"] = "Общие профили",
        ["ProcessList"] = "Профили отдельных процессов",
        ["Default"] = "По умолчанию",
        ["Remove"] = "Удалить",
        ["Reset"] = "Сбросить",
        ["Add"] = "Добавить",
        ["Help"] = "Справка",
        ["Edit"] = "Редактировать",
        ["TdpLimitsAtStartNote"] = "Внимание: включите 'Применять лимит TDP при загрузке профиля' в настройках процессора для автоматического применения настроенного лимита TDP при смене или применении профиля",
        ["ProcessProfilesHelp1"] = "Общие профили помогают быстро изменять настройки (TDP, ограничение FPS и другие).",
        ["ProcessProfilesHelp2"] = "Выберите профиль или создайте новый, нажмите 'Применить' - настройки загрузятся.",
        ["ProcessProfilesHelp3"] = "Настройки всех вкладок при их изменении автоматически сохраняются в выбранном профиле.",
        ["ProcessProfilesHelp4"] = "Можно задать настройки только для конкретного процесса (игры).",
        ["ProcessProfilesHelp5"] = "Добавьте процесс или выберите его, нажмите 'Редактировать' и настройте параметры в нужных вкладках.",
        ["ProcessProfilesHelp6"] = "При активации окна этого процесса автоматически применяются настройки из его профиля.",
        ["ProcessProfilesHelp7"] = "При завершении или сворачивании окна процесса активируется общий профиль (если не выбрано 'Сохранять последний профиль процесса').",
        // Keep the old key for compatibility

        // Dialog
        ["AddProfile"] = "Добавить профиль",
        ["EnterProfileName"] = "Введите название профиля:",
        ["ProfileName"] = "Название профиля",
        ["OK"] = "ОК",
        ["Cancel"] = "Отмена"
    };
}

// ... existing code ...

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