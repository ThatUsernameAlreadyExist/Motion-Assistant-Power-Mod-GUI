using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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

        private static readonly TimeSpan DefaultDebounceDelay = TimeSpan.FromSeconds(1);

        public static readonly AttachedProperty<ICommand> DebouncedCommandProperty =
            AvaloniaProperty.RegisterAttached<Slider, ICommand>(
                "DebouncedCommand", typeof(SliderKeyboardBehavior), null);

        public static ICommand GetDebouncedCommand(Slider slider) => slider.GetValue(DebouncedCommandProperty);
        public static void SetDebouncedCommand(Slider slider, ICommand value) => slider.SetValue(DebouncedCommandProperty, value);

        static SliderKeyboardBehavior()
        {
            DebouncedCommandProperty.Changed.AddClassHandler<Slider>(OnCommandChanged);
        }

        private static void OnCommandChanged(Slider slider, AvaloniaPropertyChangedEventArgs e)
        {
            // Unsubscribe from old
            if (e.OldValue != null)
            {
                slider.PointerPressed -= OnPointerPressed;
                slider.PointerCaptureLost -= OnPointerCaptureLost;
                slider.RemoveHandler(RangeBase.ValueChangedEvent, (EventHandler<RangeBaseValueChangedEventArgs>)OnValueChanged);
                CleanupTimer(slider);
                _isDragging.Remove(slider);
            }

            // Subscribe to new
            if (e.NewValue != null)
            {
                slider.PointerPressed += OnPointerPressed;
                slider.PointerCaptureLost += OnPointerCaptureLost;
                slider.AddHandler(RangeBase.ValueChangedEvent, (EventHandler<RangeBaseValueChangedEventArgs>)OnValueChanged);
                _isDragging[slider] = false;
            }
        }

        private static void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is Slider slider)
            {
                _isDragging[slider] = true;
                CleanupTimer(slider); // Cancel any pending debounce when starting drag
            }
        }

        private static void OnPointerCaptureLost(object sender, PointerCaptureLostEventArgs e)
        {
            if (sender is Slider slider)
            {
                _isDragging[slider] = false;
                CleanupTimer(slider);

                // Execute immediately on mouse drag complete
                ExecuteCommand(slider);
            }
        }

        private static void OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
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
            int currentValue = (int)slider.Value;

            CleanupTimer(slider);

            _debounceTimers[slider] = DispatcherTimer.RunOnce(() =>
            {
                ExecuteCommand(slider);
                _debounceTimers.Remove(slider);
            }, DefaultDebounceDelay);
        }

        private static void ExecuteCommand(Slider slider)
        {
            int value = (int)slider.Value;
            var command = GetDebouncedCommand(slider);

            if (command != null && command.CanExecute(value))
            {
                command.Execute(value);
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