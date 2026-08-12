using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
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
        private DispatcherTimer _timer;
        private readonly TimeSpan _animationDuration;

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
                    StartAnimation();
                }
            }
        }

        public AnimatedDouble(double initialValue = 0, double animationDurationSeconds = 1.0)
        {
            _value = initialValue;
            _target = initialValue;
            _animationDuration = TimeSpan.FromSeconds(animationDurationSeconds);
        }

        private void StartAnimation()
        {
            _timer?.Stop();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 fps
            };
            _timer.Tick += OnTick;
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            double diff = _target - _value;

            if (Math.Abs(diff) < 0.1)
            {
                // Close enough — snap to target and stop
                _value = _target;
                _timer?.Stop();
                OnPropertyChanged();
                return;
            }

            // Linear interpolation: move ~1/30th of the way per frame
            // Over 30 frames (1 second) this gives smooth easing
            _value += diff * (1.0 / 30.0);
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
