using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace PmGui.ViewModels
{
    /// <summary>
    /// A double value that smoothly animates from its current displayed value
    /// to a new target value over a configurable duration.
    /// </summary>
    public class AnimatedDouble : INotifyPropertyChanged
    {
        private double _value;
        private double _target;
        private bool _isAnimating;
        private readonly double _interpolationFactor;

        // Fixed registry — one per app lifetime, drives all AnimatedDouble instances.
        // Size is large enough for all gauges plus headroom.
        private static readonly AnimatedDouble[] _registry = new AnimatedDouble[32];
        private static int _registryCount;

        // Single shared timer
        private static DispatcherTimer _sharedTimer;

        public double Value
        {
            get => _value;
            private set
            {
                if (Math.Abs(_value - value) > 0.5)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Target
        {
            set
            {
                if (Math.Abs(_target - value) > 0.05)
                {
                    _target = value;
                    if (!_isAnimating)
                    {
                        _isAnimating = true;
                        EnsureTimerStarted();
                    }
                }
            }
        }

        public AnimatedDouble(double initialValue = 0, double animationDurationSeconds = 0.5)
        {
            _value = initialValue;
            _target = initialValue;
            // 30 fps - frame time ≈ 1/30 s. Factor = frameTime / duration
            _interpolationFactor = 1.0 / (30.0 * animationDurationSeconds);
            Register();
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

        private static void EnsureTimerStarted()
        {
            if (_sharedTimer == null)
            {
                _sharedTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(33) // ~30 fps
                };
                _sharedTimer.Tick += OnSharedTick;
            }
            _sharedTimer.Start();
        }

        private static void OnSharedTick(object sender, EventArgs e)
        {
            for (int i = 0; i < _registryCount; i++)
            {
                var anim = _registry[i];
                if (anim != null && anim._isAnimating)
                    anim.Tick();
            }

            // Stop timer when nothing is animating
            // If any slot is animating, keep going
            bool anyAnimating = false;
            for (int i = 0; i < _registryCount; i++)
            {
                if (_registry[i]?._isAnimating == true)
                {
                    anyAnimating = true;
                    break;
                }
            }
            if (!anyAnimating)
                _sharedTimer?.Stop();
        }

        private void Tick()
        {
            double diff = _target - _value;

            if (Math.Abs(diff) < 0.1)
            {
                _value = _target;
                _isAnimating = false;
                OnPropertyChanged();
                return;
            }

            // Linear interpolation: move _interpolationFactor per frame
            _value += diff * _interpolationFactor;
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
