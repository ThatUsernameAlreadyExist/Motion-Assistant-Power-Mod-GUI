using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Linq;
using PmGui.Models;
using PmGui.ViewModels;
using PmGui.Views;

namespace PmGui.Gamepad
{
    public enum NavigationContext
    {
        Menu,
        Page
    }

    public class GamepadNavigationManager : IDisposable
    {
        private readonly TopLevel _topLevel;
        private readonly MainWindowViewModel _viewModel;
        private GamepadService _gamepadService;
        private DispatcherTimer _repeatTimer;

        private NavigationContext _currentContext = NavigationContext.Menu;
        private GamepadButton _heldButton = GamepadButton.None;
        private DateTime _buttonPressTime;
        private DateTime _lastRepeatTime;

        private bool _isSliderEditMode;
        private Slider _activeSlider;
        private bool _disposed;

        private const int InitialRepeatDelayMs = 600;
        private const int RepeatIntervalMs = 100;

        public NavigationContext CurrentContext => _currentContext;
        public bool IsConnected => _gamepadService?.IsConnected ?? false;
        public bool IsRunning => _gamepadService?.IsRunning ?? false;

        public event EventHandler GamepadConnected;
        public event EventHandler GamepadDisconnected;

        public GamepadNavigationManager(TopLevel topLevel, MainWindowViewModel viewModel, bool autoStart = true)
        {
            _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            if (autoStart)
            {
                Start();
            }
        }

        public void Start()
        {
            if (_gamepadService?.IsRunning == true)
                return;

            _gamepadService = new GamepadService(false);
            _gamepadService.ButtonPressed += OnButtonPressed;
            _gamepadService.ButtonReleased += OnButtonReleased;
            _gamepadService.Connected += (s, e) => Dispatcher.UIThread.Post(() => GamepadConnected?.Invoke(this, EventArgs.Empty));
            _gamepadService.Disconnected += (s, e) => Dispatcher.UIThread.Post(() => GamepadDisconnected?.Invoke(this, EventArgs.Empty));
            _gamepadService.Start();

            Dispatcher.UIThread?.Post(() =>
            {
                _repeatTimer = _repeatTimer ?? new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _repeatTimer.Tick += OnRepeatTick;
                _repeatTimer.Start();
            });
        }

        public void Stop()
        {
            Dispatcher.UIThread?.Post(() => _repeatTimer?.Stop());

            var localService = _gamepadService;

            _gamepadService = null;

            if (localService != null)
            {
                localService.Stop();
                localService.Dispose();
            }

            _heldButton = GamepadButton.None;
        }

        public void SetNavigationContext(NavigationContext context)
        {
            if (_currentContext == context) return;

            _currentContext = context;

            ExitSliderEditMode();

            _isSliderEditMode = false;
            _activeSlider = null;

            Dispatcher.UIThread.Post(() =>
            {
                var window = _topLevel as MainWindow;
                if (window == null) return;

                if (context == NavigationContext.Menu)
                {
                    window.Classes.Remove("PageMode");
                    FocusSelectedMenuItem();
                }
                else
                {
                    window.Classes.Add("PageMode");
                    FocusFirstPageControl();
                }
            });
        }

        public void FocusSelectedMenuItem()
        {
            var menuScrollViewer = _topLevel.FindControl<ScrollViewer>("MenuScrollViewer");
            if (menuScrollViewer == null) return;

            var selectedItem = _viewModel.MenuItems.FirstOrDefault(m => m.IsSelected);
            if (selectedItem == null) return;

            var button = FindControl<Button>(menuScrollViewer,
                b => b.DataContext is SettingsMenuItem item && item == selectedItem);
            button?.Focus();
        }

        public void FocusFirstPageControl()
        {
            var contentScrollViewer = _topLevel.FindControl<ScrollViewer>("ContentScrollViewer");
            if (contentScrollViewer == null) return;

            var firstFocusable = FindFirstFocusable(contentScrollViewer);
            firstFocusable?.Focus(NavigationMethod.Tab, KeyModifiers.None);
        }

        private void OnButtonPressed(object sender, GamepadButtonEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _heldButton = e.Button;
                _buttonPressTime = DateTime.Now;
                _lastRepeatTime = DateTime.Now;

                ProcessButton(e.Button);
            });
        }

        private void OnButtonReleased(object sender, GamepadButtonEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_heldButton == e.Button)
                    _heldButton = GamepadButton.None;
            });
        }

        private void OnRepeatTick(object sender, EventArgs e)
        {
            if (_heldButton == GamepadButton.None) return;
            if (!IsNavigationButton(_heldButton)) return;

            var now = DateTime.Now;
            var afterPress = (now - _buttonPressTime).TotalMilliseconds;
            if (afterPress < InitialRepeatDelayMs) return;
            if ((now - _lastRepeatTime).TotalMilliseconds >= RepeatIntervalMs)
            {
                _lastRepeatTime = now;
                ProcessButton(_heldButton);
            }
        }

        private bool IsNavigationButton(GamepadButton button)
        {
            return button == GamepadButton.DPadUp ||
                   button == GamepadButton.DPadDown ||
                   button == GamepadButton.DPadLeft ||
                   button == GamepadButton.DPadRight ||
                   button == GamepadButton.LeftStickUp ||
                   button == GamepadButton.LeftStickDown ||
                   button == GamepadButton.LeftStickLeft ||
                   button == GamepadButton.LeftStickRight ||
                   button == GamepadButton.RightStickUp ||
                   button == GamepadButton.RightStickDown;
        }

        private bool IsShoulderButton(GamepadButton button)
        {
            return button == GamepadButton.LeftShoulder ||
                   button == GamepadButton.RightShoulder;
        }

        private void ProcessButton(GamepadButton button)
        {
            if (IsShoulderButton(button))
            {
                ProcessShoulderNavigation(button);
            }
            else if (button == GamepadButton.Start)
            {
                ProcessMenuExpand();
            }
            else if (button == GamepadButton.RightStickDown || button == GamepadButton.RightStickUp)
            {
                ScrollViewport(button == GamepadButton.RightStickDown ? 1 : -1);
            }
            else if (_isSliderEditMode)
            {
                ProcessSliderMode(button);
            }
            else
            {
                switch (_currentContext)
                {
                    case NavigationContext.Menu:
                        ProcessMenuNavigation(button);
                        break;
                    case NavigationContext.Page:
                        ProcessPageNavigation(button);
                        break;
                }
            }
        }

        private void ProcessMenuExpand()
        {
            _viewModel.ToggleMenu();
        }

        private void ScrollViewport(int direction)
        {
            var contentScrollViewer = _topLevel.FindControl<ScrollViewer>("ContentScrollViewer");

            if (contentScrollViewer != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        var viewport = contentScrollViewer.Viewport;
                        var currentOffset = contentScrollViewer.Offset;

                        double scrollAmount = viewport.Height * 0.3;

                        var newOffset = new Vector(currentOffset.X, currentOffset.Y + direction * scrollAmount);
                        contentScrollViewer.Offset = newOffset;
                    }
                    catch
                    {
                    }
                }, DispatcherPriority.Background);
            }
        }

        private void ProcessMenuNavigation(GamepadButton button)
        {
            switch (button)
            {
                case GamepadButton.DPadDown:
                case GamepadButton.LeftStickDown:
                    MoveMenuSelection(1);
                    break;

                case GamepadButton.DPadUp:
                case GamepadButton.LeftStickUp:
                    MoveMenuSelection(-1);
                    break;

                case GamepadButton.A:
                    SetNavigationContext(NavigationContext.Page);
                    break;
            }
        }

        private void ProcessPageNavigation(GamepadButton button)
        {
            switch (button)
            {
                case GamepadButton.DPadDown:
                case GamepadButton.DPadRight:
                case GamepadButton.LeftStickDown:
                case GamepadButton.LeftStickRight:
                    SimulateKeyPress(Key.Tab, RawInputModifiers.None);
                    break;

                case GamepadButton.DPadUp:
                case GamepadButton.DPadLeft:
                case GamepadButton.LeftStickUp:
                case GamepadButton.LeftStickLeft:
                    SimulateKeyPress(Key.Tab, RawInputModifiers.Shift);
                    break;

                case GamepadButton.A:
                    ActivateFocusedElement();
                    break;

                case GamepadButton.B:
                    HandleEscape();
                    break;
            }
        }

        private void ProcessShoulderNavigation(GamepadButton button)
        {
            int direction = button == GamepadButton.RightShoulder ? 1 : -1;
            var visiblePages = _viewModel.MenuItems.Where(m => m.IsVisible).ToList();
            var currentIndex = visiblePages.FindIndex(m => m.IsSelected);
            var newIndex = (currentIndex + direction + visiblePages.Count) % visiblePages.Count;

            _viewModel.SelectPage(visiblePages[newIndex].PageKey);

            Dispatcher.UIThread.Post(() =>
            {
                if (_currentContext == NavigationContext.Page)
                    FocusFirstPageControl();
                else
                    SetNavigationContext(NavigationContext.Page);
            }, DispatcherPriority.Background);
        }

        private void ProcessSliderMode(GamepadButton button)
        {
            if (_activeSlider == null)
            {
                ExitSliderEditMode();
                return;
            }

            switch (button)
            {
                case GamepadButton.DPadRight:
                case GamepadButton.LeftStickRight:
                case GamepadButton.DPadUp:
                case GamepadButton.LeftStickUp:
                    AdjustSlider(1);
                    break;

                case GamepadButton.DPadLeft:
                case GamepadButton.LeftStickLeft:
                case GamepadButton.DPadDown:
                case GamepadButton.LeftStickDown:
                    AdjustSlider(-1);
                    break;

                case GamepadButton.A:
                case GamepadButton.B:
                    ExitSliderEditMode(true);
                    break;
            }
        }

        private void MoveMenuSelection(int direction)
        {
            var visiblePages = _viewModel.MenuItems.Where(m => m.IsVisible).ToList();
            var currentIndex = visiblePages.FindIndex(m => m.IsSelected);
            var newIndex = (currentIndex + direction + visiblePages.Count) % visiblePages.Count;

            _viewModel.SelectPage(visiblePages[newIndex].PageKey);
            FocusSelectedMenuItem();
        }

        private void SimulateKeyPress(Key key, RawInputModifiers modifiers)
        {
            var focused = _topLevel.FocusManager?.GetFocusedElement() as InputElement;

            if (focused == null)
            {
                var firstFocusable = _currentContext == NavigationContext.Menu
                    ? FindControl<Button>(_topLevel, b => b.Classes.Contains("nav-button"))
                    : FindFirstFocusable(_topLevel);
                firstFocusable?.Focus(NavigationMethod.Tab, KeyModifiers.None);
                return;
            }

            SimulateKeyPress(focused, key, modifiers);
        }

        private KeyModifiers ConvertModifiers(RawInputModifiers raw)
        {
            var result = KeyModifiers.None;
            if ((raw & RawInputModifiers.Shift) != 0) result |= KeyModifiers.Shift;
            if ((raw & RawInputModifiers.Control) != 0) result |= KeyModifiers.Control;
            if ((raw & RawInputModifiers.Alt) != 0) result |= KeyModifiers.Alt;
            return result;
        }

        private void ActivateFocusedElement()
        {
            var focused = _topLevel.FocusManager?.GetFocusedElement();
            if (focused == null) return;

            switch (focused)
            {
                case Slider slider:
                    EnterSliderEditMode(slider);
                    break;
                case CheckBox checkBox:
                    checkBox.IsChecked = !(checkBox.IsChecked ?? false);
                    break;
                case RadioButton radioButton:
                    radioButton.IsChecked = true;
                    break;
                case ToggleButton toggleButton:
                    toggleButton.IsChecked = !toggleButton.IsChecked;
                    break;
                case Button button:
                    if (button.Command?.CanExecute(button.CommandParameter) == true)
                        button.Command.Execute(button.CommandParameter);
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
                    break;
                case ComboBox comboBox:
                    comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
                    if (!comboBox.IsDropDownOpen)
                    {
                        comboBox.Focus(NavigationMethod.Tab, KeyModifiers.None);
                    }
                    break;
                case InputElement inputElement:
                    SimulateKeyPress(inputElement, Key.Enter, RawInputModifiers.None);
                    break;
            }
        }

        private void SimulateKeyPress(InputElement control, Key key, RawInputModifiers modifiers)
        {
            if (control == null)
            {
                return;
            }

            // Create and raise KeyDown event
            var keyEventArgs = new KeyEventArgs();

            // Use reflection to set init-only properties (C# 7.3 compatibility)
            var keyProperty = typeof(KeyEventArgs).GetProperty("Key");
            keyProperty?.SetValue(keyEventArgs, key);

            var modifiersProperty = typeof(KeyEventArgs).GetProperty("KeyModifiers");
            modifiersProperty?.SetValue(keyEventArgs, ConvertModifiers(modifiers));

            keyEventArgs.RoutedEvent = InputElement.KeyDownEvent;
            keyEventArgs.Route = RoutingStrategies.Tunnel | RoutingStrategies.Bubble;

            control.RaiseEvent(keyEventArgs);
        }

        private IInputElement FindFirstFocusable(Visual parent)
        {
            if (parent is InputElement input && input.Focusable &&
                input.IsEffectivelyEnabled && input.IsEffectivelyVisible)
            {
                return input;
            }

            foreach (var child in parent.GetVisualChildren())
            {
                if (child is Visual visualChild)
                {
                    var found = FindFirstFocusable(visualChild);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private T FindControl<T>(Visual parent, Func<T, bool> predicate) where T : Visual
        {
            if (parent is T t && predicate(t))
                return t;

            foreach (var child in parent.GetVisualChildren())
            {
                if (child is Visual visualChild)
                {
                    var found = FindControl(visualChild, predicate);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private void HandleEscape()
        {
            if (_isSliderEditMode)
            {
                ExitSliderEditMode(true);
                return;
            }

            var focused = _topLevel.FocusManager?.GetFocusedElement();

            ComboBox comboBox = focused as ComboBox;
            if (comboBox == null && focused is Control control)
            {
                // Traverse up the visual tree to find parent ComboBox
                comboBox = control.FindLogicalAncestorOfType<ComboBox>();
            }

            if (comboBox != null && comboBox.IsDropDownOpen)
            {
                comboBox.IsDropDownOpen = false;
                return;
            }

            SetNavigationContext(NavigationContext.Menu);
        }

        private void EnterSliderEditMode(Slider slider)
        {
            _isSliderEditMode = true;
            _activeSlider = slider;
        }

        private void ExitSliderEditMode(bool needFocus = false)
        {
            if (needFocus && _activeSlider != null)
            {
                _activeSlider.Focus(NavigationMethod.Tab, KeyModifiers.None);
            }
            _isSliderEditMode = false;
            _activeSlider = null;
        }

        private void AdjustSlider(int direction)
        {
            if (_activeSlider == null) return;

            var now = DateTime.Now;
            var afterPress = (now - _buttonPressTime).TotalMilliseconds;

            double step = _activeSlider.SmallChange > 0 && afterPress < 1000
                ? _activeSlider.SmallChange
                : (_activeSlider.Maximum - _activeSlider.Minimum) / Math.Max(15.0, 50 - afterPress / 1000);

            double newValue = _activeSlider.Value + (step * direction);
            newValue = Math.Max(_activeSlider.Minimum, Math.Min(_activeSlider.Maximum, newValue));
            _activeSlider.Value = newValue;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}