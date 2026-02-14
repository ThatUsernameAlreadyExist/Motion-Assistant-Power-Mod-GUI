using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace PmGui.Models
{
    public static class SliderKeyboardBehavior
    {
        private static readonly Dictionary<Slider, IDisposable> _debounceTimers = new Dictionary<Slider, IDisposable>();
        private static readonly Dictionary<Slider, bool> _isDragging = new Dictionary<Slider, bool>();
        private static readonly Dictionary<Slider, double> _initialValue = new Dictionary<Slider, double>();
        private static bool _suppressEvents = false;

        private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromSeconds(1);

        public static readonly AttachedProperty<ICommand> DebouncedCommandProperty =
            AvaloniaProperty.RegisterAttached<Slider, ICommand>(
                "DebouncedCommand", typeof(SliderKeyboardBehavior), null);

        public static ICommand GetDebouncedCommand(Slider slider) => slider.GetValue(DebouncedCommandProperty);
        public static void SetDebouncedCommand(Slider slider, ICommand value) => slider.SetValue(DebouncedCommandProperty, value);

        public static void SetSilently(Action action)
        {
            _suppressEvents = true;
            try
            {
                action();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        static SliderKeyboardBehavior()
        {
            DebouncedCommandProperty.Changed.AddClassHandler<Slider>(OnCommandChanged);
        }

        private static void OnCommandChanged(Slider slider, AvaloniaPropertyChangedEventArgs e)
        {
            // Unsubscribe from old
            if (e.OldValue != null)
            {
                slider.RemoveHandler(InputElement.PointerPressedEvent, (EventHandler<PointerPressedEventArgs>)OnPointerPressed);
                slider.PointerCaptureLost -= OnPointerCaptureLost;
                slider.RemoveHandler(RangeBase.ValueChangedEvent, (EventHandler<RangeBaseValueChangedEventArgs>)OnValueChanged);
                CleanupTimer(slider);
                _isDragging.Remove(slider);
                _initialValue.Remove(slider);
            }

            if (e.NewValue != null)
            {
                slider.AddHandler(InputElement.PointerPressedEvent, (EventHandler<PointerPressedEventArgs>)OnPointerPressed,
                  RoutingStrategies.Tunnel, handledEventsToo: true);

                slider.PointerCaptureLost += OnPointerCaptureLost;
                slider.AddHandler(RangeBase.ValueChangedEvent, (EventHandler<RangeBaseValueChangedEventArgs>)OnValueChanged);
                _isDragging[slider] = false;
                _initialValue[slider] = -1.0;
            }
        }

        private static void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_suppressEvents) return;

            if (sender is Slider slider)
            {
                _isDragging[slider] = true;
                _initialValue[slider] = slider.Value;
                CleanupTimer(slider); // Cancel any pending debounce when starting drag
            }
        }

        private static void OnPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
        {
            if (_suppressEvents) return;

            if (sender is Slider slider)
            {
                _isDragging[slider] = false;

                if (Math.Abs(slider.Value - _initialValue[slider]) > 0.01)
                {
                    ScheduleDebounce(slider);

                    // If need execute immediately on mouse drag complete:
                    // CleanupTimer(slider);
                    // ExecuteCommand(slider);
                }

                _initialValue[slider] = -1.0;
            }
        }

        private static void OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_suppressEvents) return;

            if (Math.Abs(e.OldValue - e.NewValue) < 0.01)
                return;

            if (sender is Slider slider)
            {
                // Skip debounce during mouse drag - will execute on PointerCaptureLost
                if (_isDragging.TryGetValue(slider, out bool dragging) && dragging)
                {
                    return;
                }

                // Keyboard/programmatic change - use debounce
                ScheduleDebounce(slider);
            }
        }

        private static void ScheduleDebounce(Slider slider)
        {
            CleanupTimer(slider);

            _debounceTimers[slider] = DispatcherTimer.RunOnce(() =>
            {
                ExecuteCommand(slider);
                _debounceTimers.Remove(slider);
            }, DefaultDebounceDelay);
        }

        private static void ExecuteCommand(Slider slider)
        {
            if (_suppressEvents) return;

            var command = GetDebouncedCommand(slider);
            if (command != null && command.CanExecute(slider.Value))
            {
                command.Execute(slider.Value);
            }
        }

        private static void CleanupTimer(Slider slider)
        {
            if (_debounceTimers.TryGetValue(slider, out IDisposable timer))
            {
                timer.Dispose();
                _debounceTimers.Remove(slider);
            }
        }
    }
}