using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using BidirectionalPipe.ActorModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows11Settings.ViewModels;
using Windows11Settings.ViewModels.Pages;
using static BidirectionalPipe.ActorModel.ActorPipe;

namespace Windows11Settings.Managers
{
    /// <summary>
    /// Global application manager that provides centralized access to window management and cross-page communication.
    /// Accessible from any view model or page.
    /// </summary>
    public class GlobalAppManager
    {
        private static GlobalAppManager _instance;
        private static readonly object _lock = new object();
        private ActorPipe _pipeServer;
        private bool _isCmdSendingEnabled = false;

        public static GlobalAppManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GlobalAppManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private GlobalAppManager() 
        {
            _pipeServer = new ActorPipe(CurrentArgs.Length > 0 ? CurrentArgs[0] : "", 15000);

            _pipeServer.CommandReceived += (sender, e) =>
            {
                if (e.Command is ActorPipe.CommandBase command)
                {
                    string commandId = command.CommandId;
                    if (string.IsNullOrEmpty(commandId))
                        return;

                    // Find and invoke the appropriate method
                    var methodInfo = this.GetType().GetMethod($"Receive{commandId}",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (methodInfo != null)
                    {
                        // Handle different command types
                        object value = null;
                        if (command is ActorPipe.StringCommand stringCmd)
                            value = stringCmd.Data;
                        else if (command is ActorPipe.IntCommand intCmd)
                            value = intCmd.Data;
                        else if (command is ActorPipe.UintCommand uintCmd)
                            value = uintCmd.Data;
                        else if (command is ActorPipe.FloatCommand floatCmd)
                            value = floatCmd.Data;
                        else if (command is ActorPipe.ListCommand<string> stringListCmd)
                            value = stringListCmd.Data;
                        else if (command is ActorPipe.ListCommand<int> intListCmd)
                            value = intListCmd.Data;
                        else if (command is ActorPipe.ContainerCommand containerCmd)
                            value = containerCmd.Data;

                        // Invoke method with appropriate parameter
                        if (value != null)
                        {
                            var parameters = methodInfo.GetParameters();
                            if (parameters.Length == 1)
                            {
                                var paramType = parameters[0].ParameterType;
                                if (paramType == typeof(string) && value is string)
                                    methodInfo.Invoke(this, new object[] { value });
                                else if (paramType == typeof(int) && value is int)
                                    methodInfo.Invoke(this, new object[] { value });
                                else if (paramType == typeof(uint) && value is uint)
                                    methodInfo.Invoke(this, new object[] { value });
                                else if ((paramType == typeof(double) || paramType == typeof(float)) && (value is double || value is float))
                                    methodInfo.Invoke(this, new object[] { value });
                                else if (paramType == typeof(bool) && value is bool)
                                    methodInfo.Invoke(this, new object[] { value });
                                else if (paramType == typeof(bool) && value is int)
                                    methodInfo.Invoke(this, new object[] { (int)value != 0 });
                                else if (paramType == typeof(List<string>) && value is List<string>)
                                    methodInfo.Invoke(this, new object[] { value });
                                else if (paramType == typeof(List<int>) && value is List<int>)
                                    methodInfo.Invoke(this, new object[] { value });
                                else if (paramType == typeof(Dictionary<string, object>) && value is Dictionary<string, object>)
                                    methodInfo.Invoke(this, new object[] { value });
                            }
                        }
                    }
                }
            };

            _pipeServer.StatusChanged += (sender, e) =>
            {
                if (e.NewStatus == PipeStatus.Disconnected || e.NewStatus == PipeStatus.Error)
                {
                    if (!IsDebugMode)
                    {
                        ExitApplication();
                    }
                }
            };

            _pipeServer.Start();
        }

        private void SendCommand<T>(T value, [CallerMemberName] string methodName = "")
        {
            if (!IsCmdSendingEnabled) return;

            try
            {
                if (methodName.StartsWith("SendCmd"))
                {
                    var commandId = "Cmd" + methodName.Substring(7); // Remove "SendCmd" prefix

                    if (value is string stringValue)
                        _pipeServer.SendString(stringValue, commandId);
                    else if (value is int intValue)
                        _pipeServer.SendInt(intValue, commandId);
                    else if (value is uint uintValue)
                        _pipeServer.SendUint(uintValue, commandId);
                    else if (value is double doubleValue)
                        _pipeServer.SendFloat((float)doubleValue, commandId);
                    else if (value is bool boolValue)
                        _pipeServer.SendInt(boolValue ? 1 : 0, commandId);
                    else if (value is List<string> stringListValue)
                        _pipeServer.SendStringList(stringListValue, commandId);
                    else if (value is List<int> intListValue)
                        _pipeServer.SendIntList(intListValue, commandId);
                    else
                        _pipeServer.SendString(value?.ToString() ?? "", commandId);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets the current application instance
        /// </summary>
        public App CurrentApp => (App)Application.Current;

        /// <summary>
        /// Gets the main application window
        /// </summary>
        public Window MainWindow => GetMainWindow();

        /// <summary>
        /// Gets the desktop lifetime for application management
        /// </summary>
        public IClassicDesktopStyleApplicationLifetime DesktopLifetime => 
            CurrentApp?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        #region Window Management Methods

        /// <summary>
        /// Shows the main window
        /// </summary>
        public void ShowWindow()
        {
            CurrentApp?.ShowMainWindow();
        }

        /// <summary>
        /// Hides the main window
        /// </summary>
        public void HideWindow()
        {
            CurrentApp?.HideMainWindow();
        }

        /// <summary>
        /// Minimizes the main window
        /// </summary>
        public void MinimizeWindow()
        {
            CurrentApp?.MinimizeWindow();
        }

        /// <summary>
        /// Maximizes or restores the main window
        /// </summary>
        public void MaximizeWindow()
        {
            CurrentApp?.MaximizeWindow();
        }

        public void NormalizeWindow()
        {
            CurrentApp?.NormalizeWindow();
        }

        /// <summary>
        /// Closes the application
        /// </summary>
        public void ExitApplication()
        {
            DesktopLifetime?.Shutdown();
        }

        /// <summary>
        /// Activates and brings the window to foreground
        /// </summary>
        public void ActivateWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Activate();
                MainWindow.Show();
            }
        }

        public void SetWindowTitle(string value)
        {
            if (MainWindow != null)
            {
                MainWindow.Title = value;
            }
        }

        /// <summary>
        /// Gets the current window state
        /// </summary>
        public WindowState GetWindowState()
        {
            return MainWindow?.WindowState ?? WindowState.Normal;
        }

        #endregion

        #region Cross-Page Communication

        /// <summary>
        /// Reference to the MainWindow ViewModel for cross-page communication
        /// </summary>
        public MainWindowViewModel MainViewModel { get; set; }

        /// <summary>
        /// Dictionary to store references to various page view models
        /// </summary>
        private readonly Dictionary<Type, object> _pageViewModels = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a page view model for cross-page access
        /// </summary>
        /// <typeparam name="T">Type of the view model</typeparam>
        /// <param name="viewModel">Instance of the view model</param>
        public void RegisterPageViewModel<T>(T viewModel) where T : class
        {
            var type = typeof(T);
            _pageViewModels[type] = viewModel;
        }

        /// <summary>
        /// Gets a registered page view model
        /// </summary>
        /// <typeparam name="T">Type of the view model</typeparam>
        /// <returns>View model instance or null if not found</returns>
        public T GetPageViewModel<T>() where T : class
        {
            var type = typeof(T);
            return _pageViewModels.TryGetValue(type, out var viewModel) ? viewModel as T : null;
        }

        /// <summary>
        /// Executes an action on a specific page view model
        /// </summary>
        /// <typeparam name="T">Type of the view model</typeparam>
        /// <param name="action">Action to execute on the view model</param>
        public void ExecuteOnPageViewModel<T>(Action<T> action) where T : class
        {
            var viewModel = GetPageViewModel<T>();
            if (viewModel != null)
            {
                action(viewModel);
            }
        }

        #endregion

        #region Communication Commands

        public void SendCmdAddToSystemAutorun(bool value) => SendCommand(value);
        public void SendCmdDisableSystemMonitoring(bool value) => SendCommand(value);
        public void SendCmdDisableBluetoothInSleepMode(bool value) => SendCommand(value);
        public void SendCmdUseNewInterface(bool value) => SendCommand(value);
        public void SendCmdMinimizeToSystemTray(bool value) => SendCommand(value);
        public void SendCmdWindowSizeSelectedItem(string value) => SendCommand(value);
        public void SendCmdEnableGyroscope(bool value) => SendCommand(value);
        public void SendCmdAutoEnableGyroscopeOnStart(bool value) => SendCommand(value);
        public void SendCmdHighPrecisionGyroscope(bool value) => SendCommand(value);
        public void SendCmdDisableBoschAccelerometer(bool value) => SendCommand(value);
        public void SendCmdGyroscopeActivationButton(int value) => SendCommand(value);
        public void SendCmdPowerLineTdpValue(double value) => SendCommand(value);
        public void SendCmdBatteryTdpValue(double value) => SendCommand(value);
        public void SendCmdCPUBoostEnabled(bool value)
        {
            SendCommand(value);
            ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.CPUBoost = value ? "On" : "Off"; });
        }
        public void SendCmdAutoOptimizeCpuFrequencyEnabled(bool value) => SendCommand(value);
        public void SendCmdUniteBatteryAndPowerlineCPUPresetsEnabled(bool value) => SendCommand(value);
        public void SendCmdUniteBatteryAndPowerlineFPSLimitEnabled(bool value) => SendCommand(value);
        public void SendCmdLoadPresetAtStartEnabled(bool value) => SendCommand(value);
        public void SendCmdPowerLineFrequencySelectedItem(string value) => SendCommand(value);
        public void SendCmdPowerLineFpsSelectedItem(string value) => SendCommand(value);
        public void SendCmdPowerLineCpuCoresSelectedItem(string value) => SendCommand(value);
        public void SendCmdBatteryFrequencySelectedItem(string value) => SendCommand(value);
        public void SendCmdBatteryFpsSelectedItem(string value) => SendCommand(value);
        public void SendCmdBatteryCpuCoresSelectedItem(string value) => SendCommand(value);
        public void SendCmdMinGpuClockValue(double value) => SendCommand(value);
        public void SendCmdMaxGpuClockValue(double value) => SendCommand(value);
        public void SendCmdCustomGpuClocksRangeEnabled(bool value) => SendCommand(value);
        public void SendCmdOptimizationModeSelectedItem(string value) => SendCommand(value);
        public void SendCmdOptimizeGpuClocksEnabled(bool value) => SendCommand(value);
        public void SendCmdApplyCustomGpuClocks(bool value) => SendCommand(value);
        public void SendCmdResetGpu(bool value) => SendCommand(value);
        public void SendCmdEnableFanSpeedControlEnabled(bool value) => SendCommand(value);
        public void SendCmdFanSpeedPresetSelectedItem(string value) => SendCommand(value);
        public void SendCmdFanSpeedControlTypeSelectedItem(string value) => SendCommand(value);
        public void SendCmdIsFixedSpeedMode(bool value) => SendCommand(value);
        public void SendCmdIsSpeedCurveMode(bool value) => SendCommand(value);
        public void SendCmdFanSpeedValue(double value) => SendCommand(value);
        public void SendCmdTemperature45Speed(double value) => SendCommand(value);
        public void SendCmdTemperature60Speed(double value) => SendCommand(value);
        public void SendCmdTemperature70Speed(double value) => SendCommand(value);
        public void SendCmdTemperature80Speed(double value) => SendCommand(value);
        public void SendCmdDelayTimeoutValue(double value) => SendCommand(value);
        public void SendCmdEnableOSDOverlay(bool value) => SendCommand(value);
        public void SendCmdOSDTypeSelectedItem(string value) => SendCommand(value);
        public void SendCmdKeepLastProcessProfile(bool value) => SendCommand(value);
        public void SendCmdProfilesListSelectedItem(string value) => SendCommand(value);
        public void SendCmdProcessListSelectedItem(string value) => SendCommand(value);
        public void SendCmdAddProcessProfile(bool value) => SendCommand(value);
        public void SendCmdRemoveProcessProfile(string value) => SendCommand(value);
        public void SendCmdEditProcessProfile(string value) => SendCommand(value);
        public void SendCmdAddGlobalProfile(string value) => SendCommand(value);
        public void SendCmdRemoveGlobalProfile(string value) => SendCommand(value);
        public void SendCmdApplyGlobalProfile(string value) => SendCommand(value);
        public void SendCmdResetGlobalProfile(string value) => SendCommand(value);
        public void SendCmdIsVisible(bool value) => SendCommand(value);
        public void SendCmdIsMonitoringVisible(bool value) => SendCommand(value);
        public void SendCmdCloseApp(bool value) => SendCommand(value);

        // Receiving commands
        private void ReceiveCmdAddToSystemAutorun(bool value) => ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { vm.AddToSystemAutorun = value; });
        private void ReceiveCmdDisableSystemMonitoring(bool value) => ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { vm.DisableSystemMonitoring = value; });
        private void ReceiveCmdDisableBluetoothInSleepMode(bool value) => ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { vm.DisableBluetoothInSleepMode = value; });
        private void ReceiveCmdUseNewInterface(bool value) => ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { vm.UseNewInterface = value; });
        private void ReceiveCmdMinimizeToSystemTray(bool value) => ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { vm.MinimizeToSystemTray = value; });
        private void ReceiveCmdWindowSizeSelectedItem(string value) => ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { if (vm.WindowSizeItems != null) { var item = vm.WindowSizeItems.FirstOrDefault(x => x.Id == value); vm.WindowSizeSelectedItem = item; } });
        private void ReceiveCmdEnableGyroscope(bool value) => ExecuteOnPageViewModel<GyroscopePageViewModel>(vm => { vm.EnableGyroscope = value; });
        private void ReceiveCmdAutoEnableGyroscopeOnStart(bool value) => ExecuteOnPageViewModel<GyroscopePageViewModel>(vm => { vm.AutoEnableGyroscopeOnStart = value; });
        private void ReceiveCmdHighPrecisionGyroscope(bool value) => ExecuteOnPageViewModel<GyroscopePageViewModel>(vm => { vm.HighPrecisionGyroscope = value; });
        private void ReceiveCmdDisableBoschAccelerometer(bool value) => ExecuteOnPageViewModel<GyroscopePageViewModel>(vm => { vm.DisableBoschAccelerometer = value; });
        private void ReceiveCmdGyroscopeActivationButton(int value) => ExecuteOnPageViewModel<GyroscopePageViewModel>(vm => { if (vm.GyroscopeActivationButtonItems != null) { var item = vm.GyroscopeActivationButtonItems.FirstOrDefault(x => x.Value == value); vm.GyroscopeActivationButtonSelectedItem = item; } });
        private void ReceiveCmdPowerLineTdpValue(double value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.PowerLineTdpValue = value; });
        private void ReceiveCmdBatteryTdpValue(double value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.BatteryTdpValue = value; });
        private void ReceiveCmdIsPowerLineActive(bool value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.IsPowerLineActive = value; });
        private void ReceiveCmdIsBatteryActive(bool value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.IsBatteryActive = value; });
        private void ReceiveCmdTdpMaxValue(double value)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.TdpMaxValue = value; });
            ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.PackagePowerMax = value; });
        }
        private void ReceiveCmdCPUBoostEnabled(bool value)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.CPUBoostEnabled = value; });
            ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.CPUBoost = value ? "On" : "Off"; });
        }
        private void ReceiveCmdAutoOptimizeCpuFrequencyEnabled(bool value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.AutoOptimizeCpuFrequencyEnabled = value; });
        private void ReceiveCmdUniteBatteryAndPowerlineCPUPresetsEnabled(bool value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.UniteBatteryAndPowerlineCPUPresetsEnabled = value; });
        private void ReceiveCmdUniteBatteryAndPowerlineFPSLimitEnabled(bool value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.UniteBatteryAndPowerlineFPSLimitEnabled = value; });
        private void ReceiveCmdLoadPresetAtStartEnabled(bool value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.LoadPresetAtStartEnabled = value; });
        private void ReceiveCmdPowerLineFrequencySelectedItem(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { if (vm.PowerLineFrequencyItems != null) { var item = vm.PowerLineFrequencyItems.FirstOrDefault(x => x.Id == value); vm.PowerLineFrequencySelectedItem = item; } });
        private void ReceiveCmdPowerLineFpsSelectedItem(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { if (vm.PowerLineFpsItems != null) { var item = vm.PowerLineFpsItems.FirstOrDefault(x => x.Id == value); vm.PowerLineFpsSelectedItem = item; } });
        private void ReceiveCmdPowerLineCpuCoresSelectedItem(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { if (vm.PowerLineCpuCoresItems != null) { var item = vm.PowerLineCpuCoresItems.FirstOrDefault(x => x.Id == value); vm.PowerLineCpuCoresSelectedItem = item; } });
        private void ReceiveCmdBatteryFrequencySelectedItem(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { if (vm.BatteryFrequencyItems != null) { var item = vm.BatteryFrequencyItems.FirstOrDefault(x => x.Id == value); vm.BatteryFrequencySelectedItem = item; } });
        private void ReceiveCmdBatteryFpsSelectedItem(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { if (vm.BatteryFpsItems != null) { var item = vm.BatteryFpsItems.FirstOrDefault(x => x.Id == value); vm.BatteryFpsSelectedItem = item; } });
        private void ReceiveCmdBatteryCpuCoresSelectedItem(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { if (vm.BatteryCpuCoresItems != null) { var item = vm.BatteryCpuCoresItems.FirstOrDefault(x => x.Id == value); vm.BatteryCpuCoresSelectedItem = item; } });
        private void ReceiveCmdMinGpuClockValue(double value) => ExecuteOnPageViewModel<GPUPageViewModel>(vm => { vm.MinGpuClockValue = value; });
        private void ReceiveCmdGpuModelName(string value) => ExecuteOnPageViewModel<GPUPageViewModel>(vm => { vm.GPUModel = value; });
        private void ReceiveCmdMaxGpuClockValue(double value) => ExecuteOnPageViewModel<GPUPageViewModel>(vm => { vm.MaxGpuClockValue = value; });
        private void ReceiveCmdCustomGpuClocksRangeEnabled(bool value) => ExecuteOnPageViewModel<GPUPageViewModel>(vm => { vm.CustomGpuClocksRangeEnabled = value; });
        private void ReceiveCmdOptimizationModeSelectedItem(string value) => ExecuteOnPageViewModel<GPUPageViewModel>(vm => { if (vm.OptimizationModeItems != null) { var item = vm.OptimizationModeItems.FirstOrDefault(x => x.Id == value); vm.OptimizationModeSelectedItem = item; } });
        private void ReceiveCmdOptimizeGpuClocksEnabled(bool value) => ExecuteOnPageViewModel<GPUPageViewModel>(vm => { vm.OptimizeGpuClocksEnabled = value; });
        private void ReceiveCmdEnableFanSpeedControlEnabled(bool value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.EnableFanSpeedControlEnabled = value; });
        private void ReceiveCmdFanSpeedPresetSelectedItem(string value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { if (vm.FanSpeedPresetItems != null) { var item = vm.FanSpeedPresetItems.FirstOrDefault(x => x.Id == value); vm.FanSpeedPresetSelectedItem = item; } });
        private void ReceiveCmdFanSpeedControlTypeSelectedItem(string value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { if (vm.FanSpeedControlTypeItems != null) { var item = vm.FanSpeedControlTypeItems.FirstOrDefault(x => x.Id == value); vm.FanSpeedControlTypeSelectedItem = item; } });
        private void ReceiveCmdIsFixedSpeedMode(bool value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.IsFixedSpeedMode = value; });
        private void ReceiveCmdIsSpeedCurveMode(bool value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.IsSpeedCurveMode = value; });
        private void ReceiveCmdFanSpeedValue(double value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.FanSpeedValue = value; });
        private void ReceiveCmdTemperature45Speed(double value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.Temperature45Speed = value; });
        private void ReceiveCmdTemperature60Speed(double value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.Temperature60Speed = value; });
        private void ReceiveCmdTemperature70Speed(double value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.Temperature70Speed = value; });
        private void ReceiveCmdTemperature80Speed(double value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.Temperature80Speed = value; });
        private void ReceiveCmdDelayTimeoutValue(double value) => ExecuteOnPageViewModel<FanPageViewModel>(vm => { vm.DelayTimeoutValue = value; });
        private void ReceiveCmdEnableOSDOverlay(bool value) => ExecuteOnPageViewModel<OSDOverlayPageViewModel>(vm => { vm.EnableOSDOverlay = value; });
        private void ReceiveCmdOSDTypeSelectedItem(string value) => ExecuteOnPageViewModel<OSDOverlayPageViewModel>(vm => { if (vm.OSDTypeItems != null) { var item = vm.OSDTypeItems.FirstOrDefault(x => x.Id == value); vm.OSDTypeSelectedItem = item; } });
        private void ReceiveCmdKeepLastProcessProfile(bool value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.KeepLastProcessProfile = value; });
        private void ReceiveCmdProfilesListSelectedItem(string value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { if (vm.ProfilesListItems != null) { var item = vm.ProfilesListItems.FirstOrDefault(x => x.Id == value); vm.ProfilesListSelectedItem = item; } });
        private void ReceiveCmdProcessListSelectedItem(string value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { if (vm.ProcessListItems != null) { var item = vm.ProcessListItems.FirstOrDefault(x => x.Id == value); vm.ProcessListSelectedItem = item; } });
        private void ReceiveCmdPackagePower(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.PackagePower = value; });
        private void ReceiveCmdPackagePowerMax(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.PackagePowerMax = value; });
        private void ReceiveCmdCpuTemperature(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.CpuTemperature = value; });
        private void ReceiveCmdCpuTemperatureMax(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.CpuTemperatureMax = value; });
        private void ReceiveCmdCpuUsage(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.CpuUsage = value; });
        private void ReceiveCmdGpuUsage(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.GpuUsage = value; });
        private void ReceiveCmdFanSpeed(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.FanSpeed = value; });
        private void ReceiveCmdFanSpeedMax(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.FanSpeedMax = value; });
        private void ReceiveCmdBatteryPower(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.BatteryPower = value; });
        private void ReceiveCmdBatteryPowerMax(double value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.BatteryPowerMax = value; });
        private void ReceiveCmdGPULockClock(uint value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.GPULockClock = value; });
        private void ReceiveCmdCpuModelName(string value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.CPUModel = value; });
        private void ReceiveCmdCPUBoost(string value) => ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.CPUBoost = value; });
        private void ReceiveCmdTDPLimit(double value)
        {
            ExecuteOnPageViewModel<MonitoringPageViewModel>(vm => { vm.TDPLimit = value.ToString(); });
            ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.ActualTdpValue = value; });
        }

        private void ReceiveCmdCommandsSendingEnabled(bool value)
        {
            _isCmdSendingEnabled = value;
        }

        private void ReceiveCmdSetProfiles(List<string> value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.SetProfiles(value); });
        private void ReceiveCmdSetProfileProcesses(List<string> value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.SetProcesses(value); });
        private void ReceiveCmdTdpPresetValues(List<int> value) => ExecuteOnPageViewModel<CPUPageViewModel>(vm => { vm.TdpPresetValues = value; });
        private void ReceiveCmdCurrentActiveProfile(string value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.CurrentActiveProfile = value; });
        private void ReceiveCmdCurrentEditingProfile(string value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.CurrentEditingProfile = value; });
        private void ReceiveCmdCurrentGlobalProfile(string value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.CurrentGlobalProfile = value; });
        private void ReceiveCmdCurrentProcessProfile(string value) => ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm => { vm.CurrentProcessProfile = value; });
        private void ReceiveCmdShowWindow(bool value)
        {
            if (value)
            {
                ShowWindow();
            }
            else
            {
                HideWindow();
            }
        }

        private void ReceiveCmdMaximizeWindow(bool value)
        {
            if (value)
            {
                MaximizeWindow();
            }
        }

        private void ReceiveCmdMinimizeWindow(bool value)
        {
            if (value)
            {
                MinimizeWindow();
            }
        }

        private void ReceiveCmdNormalizeWindow(bool value)
        {
            if (value)
            {
                NormalizeWindow();
            }
        }

        private void ReceiveCmdActivateWindow(bool value)
        {
            if (value)
            {
                ActivateWindow();
            }
        }

        private void ReceiveCmdSetWindowTitle(string value)
        {
            if (value != null && value.Length > 0)
            {
                SetWindowTitle(value);
                ExecuteOnPageViewModel<AdvancedPageViewModel>(vm => { vm.SetAppVersions(value); });
            }
        }

        #endregion

        public void SetAllControlsReadOnlyState(bool value)
        {
            // Advanced Page Read-Only Methods
            ReceiveReadOnlyMinimizeToSystemTray(value);
            ReceiveReadOnlyAddToSystemAutorun(value);
            ReceiveReadOnlyDisableSystemMonitoring(value);
            ReceiveReadOnlyDisableBluetoothInSleepMode(value);
            ReceiveReadOnlyUseNewInterface(value);
            ReceiveReadOnlyWindowSizeSelectedItem(value);

            // CPU Page Read-Only Methods
            ReceiveReadOnlyPowerLineTdpValue(value);
            ReceiveReadOnlyBatteryTdpValue(value);
            ReceiveReadOnlyIsPowerLineActive(value);
            ReceiveReadOnlyCPUBoostEnabled(value);
            ReceiveReadOnlyAutoOptimizeCpuFrequencyEnabled(value);
            ReceiveReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled(value);
            ReceiveReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled(value);
            ReceiveReadOnlyLoadPresetAtStartEnabled(value);
            ReceiveReadOnlyPowerLineFrequencySelectedItem(value);
            ReceiveReadOnlyPowerLineFpsSelectedItem(value);
            ReceiveReadOnlyPowerLineCpuCoresSelectedItem(value);
            ReceiveReadOnlyBatteryFrequencySelectedItem(value);
            ReceiveReadOnlyBatteryFpsSelectedItem(value);
            ReceiveReadOnlyBatteryCpuCoresSelectedItem(value);

            // GPU Page Read-Only Methods
            ReceiveReadOnlyMinGpuClockValue(value);
            ReceiveReadOnlyMaxGpuClockValue(value);
            ReceiveReadOnlyCustomGpuClocksRangeEnabled(value);
            ReceiveReadOnlyOptimizationModeSelectedItem(value);
            ReceiveReadOnlyOptimizeGpuClocksEnabled(value);

            // Fan Page Read-Only Methods
            ReceiveReadOnlyEnableFanSpeedControlEnabled(value);
            ReceiveReadOnlyFanSpeedPresetSelectedItem(value);
            ReceiveReadOnlyFanSpeedControlTypeSelectedItem(value);
            ReceiveReadOnlyIsFixedSpeedMode(value);
            ReceiveReadOnlyIsSpeedCurveMode(value);
            ReceiveReadOnlyFanSpeedValue(value);
            ReceiveReadOnlyTemperature45Speed(value);
            ReceiveReadOnlyTemperature60Speed(value);
            ReceiveReadOnlyTemperature70Speed(value);
            ReceiveReadOnlyTemperature80Speed(value);
            ReceiveReadOnlyDelayTimeoutValue(value);

            // Gyroscope Page Read-Only Methods
            ReceiveReadOnlyEnableGyroscope(value);
            ReceiveReadOnlyAutoEnableGyroscopeOnStart(value);
            ReceiveReadOnlyHighPrecisionGyroscope(value);
            ReceiveReadOnlyDisableBoschAccelerometer(value);
            ReceiveReadOnlyGyroscopeActivationButton(value);

            // OSD Overlay Page Read-Only Methods
            ReceiveReadOnlyEnableOSDOverlay(value);
            ReceiveReadOnlyOSDTypeSelectedItem(value);

            // Process Profiles Page Read-Only Methods
            ReceiveReadOnlyKeepLastProcessProfile(value);
            ReceiveReadOnlyProfilesListSelectedItem(value);
            ReceiveReadOnlyProcessListSelectedItem(value);
        }

        // Advanced Page Read-Only Methods
        public void ReceiveReadOnlyMinimizeToSystemTray(bool state)
        {
            ExecuteOnPageViewModel<AdvancedPageViewModel>(vm =>
            {
                vm.IsReadOnlyMinimizeToSystemTray = state;
            });
        }

        public void ReceiveReadOnlyAddToSystemAutorun(bool state)
        {
            ExecuteOnPageViewModel<AdvancedPageViewModel>(vm =>
            {
                vm.IsReadOnlyAddToSystemAutorun = state;
            });
        }

        public void ReceiveReadOnlyDisableSystemMonitoring(bool state)
        {
            ExecuteOnPageViewModel<AdvancedPageViewModel>(vm =>
            {
                vm.IsReadOnlyDisableSystemMonitoring = state;
            });
        }

        public void ReceiveReadOnlyDisableBluetoothInSleepMode(bool state)
        {
            ExecuteOnPageViewModel<AdvancedPageViewModel>(vm =>
            {
                vm.IsReadOnlyDisableBluetoothInSleepMode = state;
            });
        }

        public void ReceiveReadOnlyUseNewInterface(bool state)
        {
            ExecuteOnPageViewModel<AdvancedPageViewModel>(vm =>
            {
                vm.IsReadOnlyUseNewInterface = state;
            });
        }

        public void ReceiveReadOnlyWindowSizeSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<AdvancedPageViewModel>(vm =>
            {
                vm.IsReadOnlyWindowSizeSelectedItem = state;
            });
        }

        // CPU Page Read-Only Methods
        public void ReceiveReadOnlyPowerLineTdpValue(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyPowerLineTdpValue = state;
            });
        }

        public void ReceiveReadOnlyBatteryTdpValue(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyBatteryTdpValue = state;
            });
        }

        // Replace all ReceiveReadOnly* methods with bool arguments
        public void ReceiveReadOnlyIsPowerLineActive(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyIsPowerLineActive = state;
            });
        }

        public void ReceiveReadOnlyCPUBoostEnabled(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyCPUBoostEnabled = state;
            });
        }

        public void ReceiveReadOnlyAutoOptimizeCpuFrequencyEnabled(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyAutoOptimizeCpuFrequencyEnabled = state;
            });
        }

        public void ReceiveReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyUniteBatteryAndPowerlineCPUPresetsEnabled = state;
            });
        }

        public void ReceiveReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyUniteBatteryAndPowerlineFPSLimitEnabled = state;
            });
        }

        public void ReceiveReadOnlyLoadPresetAtStartEnabled(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyLoadPresetAtStartEnabled = state;
            });
        }

        public void ReceiveReadOnlyPowerLineFrequencySelectedItem(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyPowerLineFrequencySelectedItem = state;
            });
        }

        public void ReceiveReadOnlyPowerLineFpsSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyPowerLineFpsSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyPowerLineCpuCoresSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyPowerLineCpuCoresSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyBatteryFrequencySelectedItem(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyBatteryFrequencySelectedItem = state;
            });
        }

        public void ReceiveReadOnlyBatteryFpsSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyBatteryFpsSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyBatteryCpuCoresSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<CPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyBatteryCpuCoresSelectedItem = state;
            });
        }

        // GPU Page Read-Only Methods
        public void ReceiveReadOnlyMinGpuClockValue(bool state)
        {
            ExecuteOnPageViewModel<GPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyMinGpuClockValue = state;
            });
        }

        public void ReceiveReadOnlyMaxGpuClockValue(bool state)
        {
            ExecuteOnPageViewModel<GPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyMaxGpuClockValue = state;
            });
        }

        public void ReceiveReadOnlyCustomGpuClocksRangeEnabled(bool state)
        {
            ExecuteOnPageViewModel<GPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyCustomGpuClocksRangeEnabled = state;
            });
        }

        public void ReceiveReadOnlyOptimizationModeSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<GPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyOptimizationModeSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyOptimizeGpuClocksEnabled(bool state)
        {
            ExecuteOnPageViewModel<GPUPageViewModel>(vm =>
            {
                vm.IsReadOnlyOptimizeGpuClocksEnabled = state;
            });
        }

        // Fan Page Read-Only Methods
        public void ReceiveReadOnlyEnableFanSpeedControlEnabled(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyEnableFanSpeedControlEnabled = state;
            });
        }

        public void ReceiveReadOnlyFanSpeedPresetSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyFanSpeedPresetSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyFanSpeedControlTypeSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyFanSpeedControlTypeSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyIsFixedSpeedMode(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyIsFixedSpeedMode = state;
            });
        }

        public void ReceiveReadOnlyIsSpeedCurveMode(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyIsSpeedCurveMode = state;
            });
        }

        public void ReceiveReadOnlyFanSpeedValue(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyFanSpeedValue = state;
            });
        }

        public void ReceiveReadOnlyTemperature45Speed(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyTemperature45Speed = state;
            });
        }

        public void ReceiveReadOnlyTemperature60Speed(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyTemperature60Speed = state;
            });
        }

        public void ReceiveReadOnlyTemperature70Speed(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyTemperature70Speed = state;
            });
        }

        public void ReceiveReadOnlyTemperature80Speed(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyTemperature80Speed = state;
            });
        }

        public void ReceiveReadOnlyDelayTimeoutValue(bool state)
        {
            ExecuteOnPageViewModel<FanPageViewModel>(vm =>
            {
                vm.IsReadOnlyDelayTimeoutValue = state;
            });
        }

        // Gyroscope Page Read-Only Methods
        public void ReceiveReadOnlyEnableGyroscope(bool state)
        {
            ExecuteOnPageViewModel<GyroscopePageViewModel>(vm =>
            {
                vm.IsReadOnlyEnableGyroscope = state;
            });
        }

        public void ReceiveReadOnlyAutoEnableGyroscopeOnStart(bool state)
        {
            ExecuteOnPageViewModel<GyroscopePageViewModel>(vm =>
            {
                vm.IsReadOnlyAutoEnableGyroscopeOnStart = state;
            });
        }

        public void ReceiveReadOnlyHighPrecisionGyroscope(bool state)
        {
            ExecuteOnPageViewModel<GyroscopePageViewModel>(vm =>
            {
                vm.IsReadOnlyHighPrecisionGyroscope = state;
            });
        }

        public void ReceiveReadOnlyDisableBoschAccelerometer(bool state)
        {
            ExecuteOnPageViewModel<GyroscopePageViewModel>(vm =>
            {
                vm.IsReadOnlyDisableBoschAccelerometer = state;
            });
        }

        public void ReceiveReadOnlyGyroscopeActivationButton(bool state)
        {
            ExecuteOnPageViewModel<GyroscopePageViewModel>(vm =>
            {
                vm.IsReadOnlyGyroscopeActivationButton = state;
            });
        }

        // OSD Overlay Page Read-Only Methods
        public void ReceiveReadOnlyEnableOSDOverlay(bool state)
        {
            ExecuteOnPageViewModel<OSDOverlayPageViewModel>(vm =>
            {
                vm.IsReadOnlyEnableOSDOverlay = state;
            });
        }

        public void ReceiveReadOnlyOSDTypeSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<OSDOverlayPageViewModel>(vm =>
            {
                vm.IsReadOnlyOSDTypeSelectedItem = state;
            });
        }

        // Process Profiles Page Read-Only Methods
        public void ReceiveReadOnlyKeepLastProcessProfile(bool state)
        {
            ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm =>
            {
                vm.IsReadOnlyKeepLastProcessProfile = state;
            });
        }

        public void ReceiveReadOnlyProfilesListSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm =>
            {
                vm.IsReadOnlyProfilesListSelectedItem = state;
            });
        }

        public void ReceiveReadOnlyProcessListSelectedItem(bool state)
        {
            ExecuteOnPageViewModel<ProcessProfilesPageViewModel>(vm =>
            {
                vm.IsReadOnlyProcessListSelectedItem = state;
            });
        }

        #region Settings Management

        /// <summary>
        /// Called when global settings have changed
        /// </summary>
        public void OnSettingsChanged()
        {
            // This method can be extended to notify specific view models about settings changes
            // For now, it's a placeholder for future expansion
            System.Diagnostics.Debug.WriteLine("Global settings have been changed");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the main window instance
        /// </summary>
        private Window GetMainWindow()
        {
            return DesktopLifetime?.MainWindow;
        }

        /// <summary>
        /// Checks if the application is in hidden mode
        /// </summary>
        public bool IsHiddenMode => 
            Environment.GetEnvironmentVariable("MAPM_031125_HIDDEN_MODE") == "true";

        /// <summary>
        /// Gets the current application arguments
        /// </summary>
        public string[] CurrentArgs => DesktopLifetime?.Args ?? Array.Empty<string>();

        /// <summary>
        /// Checks if the application is running in debug mode
        /// </summary>
        public bool IsDebugMode => 
            CurrentArgs.Length > 0 && CurrentArgs[0].Equals("debug", StringComparison.OrdinalIgnoreCase);


        public bool IsCmdSendingEnabled
        {
            get => _isCmdSendingEnabled;
            set => _isCmdSendingEnabled = value;
        }
        #endregion
    }

    /// <summary>
    /// Extension methods to make GlobalAppManager easier to use from ViewModels
    /// </summary>
    public static class GlobalAppManagerExtensions
    {
        /// <summary>
        /// Convenience method to access GlobalAppManager from ViewModels
        /// </summary>
        public static GlobalAppManager App(this object _) => GlobalAppManager.Instance;
    }
}