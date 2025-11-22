using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Windows11Settings.Resources.Localization;
using Windows11Settings.ViewModels;
using Windows11Settings.ViewModels.Pages;
using Windows11Settings.Managers;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Reflection;

namespace Windows11Settings.Views.Pages
{
    public partial class FanPage : UserControl
    {
        private Canvas _curveCanvas;
        private Grid _chartArea;
        private Polyline _curveLine;

        public FanPage()
        {
            InitializeComponent();
            
            // Subscribe to localization changes
            LocalizationManager.Instance.PropertyChanged += OnLocalizationChanged;
            
            // Initialize debounce timer
            InitializeDebounceTimer();
        }

        private void OnLocalizationChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Refresh view model when language changes
            if (DataContext is FanPageViewModel viewModel)
            {
                viewModel.RefreshTranslations();
            }
        }
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            // Initialize view model if not already set through parent window
            if (DataContext == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    DataContext = mainWindowVm.FanPageViewModel;
                }
            }

            // Find the canvas and chart area
            _curveCanvas = this.FindControl<Canvas>("CurveCanvas");
            _chartArea = this.FindControl<Grid>("ChartArea");

            // Initial curve draw
            UpdateCurve();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            
            // Unsubscribe from localization changes
            LocalizationManager.Instance.PropertyChanged -= OnLocalizationChanged;
            
            // Clean up debounce timer
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        private void OnSliderValueChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            // Update curve when any slider value changes
            if (e.Property.Name == "Value")
            {
                UpdateCurve();
            }
        }

        private void OnChartAreaSizeChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            // Redraw curve when chart area is resized
            if (e.Property.Name == "Bounds")
            {
                UpdateCurve();
            }
        }

        private void UpdateCurve()
        {
            if (_curveCanvas == null || _chartArea == null)
                return;

            // Get the main window view model
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainWindowVm)
                {
                    var fanVm = mainWindowVm.FanPageViewModel;
                    
                    // Get actual chart area dimensions
                    var chartWidth = _chartArea.Bounds.Width;
                    var chartHeight = _chartArea.Bounds.Height;

                    if (chartWidth <= 0 || chartHeight <= 0)
                        return;

                    // Calculate positions for 4 temperature points
                    var margin = 20.0;
                    var usableWidth = chartWidth - (2 * margin);
                    var usableHeight = chartHeight - (2 * margin);
                    var columnWidth = usableWidth / 4;

                    // Create points for the curve (inverted Y because 0 is at top)
                    var points = new List<Point>
                    {
                        new Point(margin + columnWidth * 0.5, usableHeight - ((fanVm.Temperature45Speed - fanVm.FanSpeedMinValue) / (fanVm.FanSpeedMaxValue - fanVm.FanSpeedMinValue) * usableHeight) + margin),
                        new Point(margin + columnWidth * 1.5, usableHeight - ((fanVm.Temperature60Speed - fanVm.FanSpeedMinValue) / (fanVm.FanSpeedMaxValue - fanVm.FanSpeedMinValue) * usableHeight) + margin),
                        new Point(margin + columnWidth * 2.5, usableHeight - ((fanVm.Temperature70Speed - fanVm.FanSpeedMinValue) / (fanVm.FanSpeedMaxValue - fanVm.FanSpeedMinValue) * usableHeight) + margin),
                        new Point(margin + columnWidth * 3.5, usableHeight - ((fanVm.Temperature80Speed - fanVm.FanSpeedMinValue) / (fanVm.FanSpeedMaxValue - fanVm.FanSpeedMinValue) * usableHeight) + margin)
                    };

                    // Clear canvas and create new polyline for the curve
                    _curveCanvas.Children.Clear();
                    
                    _curveLine = new Polyline
                    {
                        Points = new Points(points),
                        Stroke = new SolidColorBrush(Color.Parse("#0078D4")),
                        StrokeThickness = 3,
                        StrokeLineCap = PenLineCap.Round,
                        StrokeJoin = PenLineJoin.Round
                    };

                    _curveCanvas.Children.Add(_curveLine);
                }
            }
        }

        // Debounce timer for slider value changes
        private System.Timers.Timer _debounceTimer;
        private readonly Dictionary<string, Action> _pendingActions = new Dictionary<string, Action>();

        private void InitializeDebounceTimer()
        {
            _debounceTimer = new System.Timers.Timer(300); // 300ms debounce
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
            _debounceTimer.AutoReset = false;
        }

        private void OnDebounceTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                foreach (var action in _pendingActions.Values)
                {
                    action?.Invoke();
                }
                _pendingActions.Clear();
            });
        }

        private void QueueAction(string key, Action action)
        {
            _pendingActions[key] = action;
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private void OnFanSpeedDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider && slider.DataContext is MainWindowViewModel mainWindowVm)
            {
                var fanVm = mainWindowVm.FanPageViewModel;
                QueueAction("FanSpeed", () => GlobalAppManager.Instance.SendCmdFanSpeedValue(fanVm.FanSpeedValue));
            }
        }

        private void SendFanCurveValues(FanPageViewModel fanVm)
        {
            QueueAction("Temperature45", () => GlobalAppManager.Instance.SendCmdTemperature45Speed(fanVm.Temperature45Speed));
            QueueAction("Temperature60", () => GlobalAppManager.Instance.SendCmdTemperature60Speed(fanVm.Temperature60Speed));
            QueueAction("Temperature70", () => GlobalAppManager.Instance.SendCmdTemperature70Speed(fanVm.Temperature70Speed));
            QueueAction("Temperature80", () => GlobalAppManager.Instance.SendCmdTemperature80Speed(fanVm.Temperature80Speed));
        }

        private void OnTemperature45DragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider && slider.DataContext is MainWindowViewModel mainWindowVm)
            {
                var fanVm = mainWindowVm.FanPageViewModel;
                SendFanCurveValues(fanVm);
            }
        }

        private void OnTemperature60DragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider && slider.DataContext is MainWindowViewModel mainWindowVm)
            {
                var fanVm = mainWindowVm.FanPageViewModel;
                SendFanCurveValues(fanVm);
            }
        }

        private void OnTemperature70DragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider && slider.DataContext is MainWindowViewModel mainWindowVm)
            {
                var fanVm = mainWindowVm.FanPageViewModel;
                SendFanCurveValues(fanVm);
            }
        }

        private void OnTemperature80DragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider && slider.DataContext is MainWindowViewModel mainWindowVm)
            {
                var fanVm = mainWindowVm.FanPageViewModel;
                SendFanCurveValues(fanVm);
            }
        }

        private void OnDelayTimeoutDragCompleted(object sender, Avalonia.Input.PointerCaptureLostEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Event fired: {MethodBase.GetCurrentMethod()?.Name}");
            if (sender is Slider slider && slider.DataContext is MainWindowViewModel mainWindowVm)
            {
                var fanVm = mainWindowVm.FanPageViewModel;
                QueueAction("DelayTimeout", () => GlobalAppManager.Instance.SendCmdDelayTimeoutValue(fanVm.DelayTimeoutValue));
            }
        }
    }
}
