using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace PmGui.ViewModels
{
    /// <summary>
    /// A double value that smoothly animates from its current displayed value
    /// to a new target value over a configurable duration using ease-out cubic.
    /// </summary>
    public class AnimatedDouble : INotifyPropertyChanged
    {
        private double _value;
        private double _target;
        private double _startValue;
        private bool _isAnimating;
        private readonly double _duration;
        private readonly double _displayStep;
        private readonly double _frameTime;
        private double _elapsed;
        private long _lastNotifiedDisplayStep;

        // ── Shared registry: drives every AnimatedDouble with a single timer ──
        private static readonly AnimatedDouble[] _registry = new AnimatedDouble[32];
        private static int _registryCount;
        private static bool _isPaused;

        // Single shared timer – 20 fps (50 ms interval)
        private static DispatcherTimer _sharedTimer;
        private const int TimerIntervalMs = 50;

        /// <summary>Current (possibly mid-animation) value.</summary>
        public double Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Target value to animate towards. Setting a new target restarts the
        /// animation from the current interpolated value.
        /// </summary>
        public double Target
        {
            set
            {
                if (Math.Abs(_target - value) > 0.01)
                {
                    _startValue = _value;
                    _target = value;
                    _elapsed = 0;
                    if (!_isAnimating)
                    {
                        _isAnimating = true;
                        EnsureTimerStarted();
                    }
                }
            }
        }

        /// <param name="initialValue">Initial value.</param>
        /// <param name="animationDurationSeconds">Duration of the ease-out animation.</param>
        /// <param name="displayStep">
        /// Minimum visible change (display precision). PropertyChanged is only raised
        /// when the value crosses a multiple of this step.
        /// Use 1.0 for integer displays (%, RPM, °C), 0.1 for one-decimal displays (W).
        /// </param>
        public AnimatedDouble(double initialValue = 0, double animationDurationSeconds = 0.5, double displayStep = 1.0)
        {
            _value = initialValue;
            _target = initialValue;
            _startValue = initialValue;
            _duration = animationDurationSeconds;
            _displayStep = displayStep;
            _frameTime = TimerIntervalMs / 1000.0;
            _elapsed = 0;
            _lastNotifiedDisplayStep = GetDisplayStep(initialValue);
            Register();
        }

        // Quantizes a raw value to the display-step grid so we can detect
        // when the user-visible number actually changed.
        private long GetDisplayStep(double value)
        {
            // Small epsilon guards against IEEE-754 boundary artifacts
            // (e.g. 5.3 / 0.1 → 52.99999… instead of 53).
            return (long)Math.Floor(value / _displayStep + 1e-9);
        }

        private void Register()
        {
            for (int i = 0; i < _registry.Length; i++)
            {
                if (_registry[i] == null)
                {
                    _registry[i] = this;
                    _registryCount++;
                    return;
                }
            }
        }

        /// <summary>
        /// Pause or resume all animations.
        /// When paused the shared timer stops immediately.
        /// When resumed the timer restarts if any instance is still animating.
        /// </summary>
        public static void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (paused)
            {
                if (_sharedTimer != null)
                    _sharedTimer.Stop();
            }
            else
            {
                EnsureTimerStarted();
            }
        }

        private static void EnsureTimerStarted()
        {
            if (_isPaused) return;

            if (_sharedTimer == null)
            {
                _sharedTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(TimerIntervalMs)
                };
                _sharedTimer.Tick += OnSharedTick;
            }
            _sharedTimer.Start();
        }

        /// <summary>
        /// Single pass: tick every animating instance, then stop the timer
        /// if nothing is still animating.
        /// </summary>
        private static void OnSharedTick(object sender, EventArgs e)
        {
            bool anyAnimating = false;
            for (int i = 0; i < _registryCount; i++)
            {
                var anim = _registry[i];
                if (anim != null && anim._isAnimating)
                {
                    anim.Tick();
                    if (anim._isAnimating)
                        anyAnimating = true;
                }
            }
            if (!anyAnimating)
            {
                if (_sharedTimer != null)
                    _sharedTimer.Stop();
            }
        }

        private void Tick()
        {
            _elapsed += _frameTime;
            double progress = _elapsed / _duration;

            if (progress >= 1.0)
            {
                // Snap to final value and stop.
                _value = _target;
                _isAnimating = false;

                long finalStep = GetDisplayStep(_value);
                if (finalStep != _lastNotifiedDisplayStep)
                {
                    _lastNotifiedDisplayStep = finalStep;
                    OnPropertyChanged();
                }
                return;
            }

            // Ease-out cubic: fast start, smooth deceleration.
            double inv = 1.0 - progress;
            double t = 1.0 - inv * inv * inv;
            _value = _startValue + (_target - _startValue) * t;

            // Only notify when the displayed (quantized) value actually changes.
            long newDisplayStep = GetDisplayStep(_value);
            if (newDisplayStep != _lastNotifiedDisplayStep)
            {
                _lastNotifiedDisplayStep = newDisplayStep;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
