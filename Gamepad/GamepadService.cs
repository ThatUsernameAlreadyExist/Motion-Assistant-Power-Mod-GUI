using System;
using System.Diagnostics;
using System.Threading;
using SharpDX.XInput;

namespace Windows11Settings.Gamepad
{
    public enum GamepadButton
    {
        None,
        A, B, X, Y,
        DPadUp, DPadDown, DPadLeft, DPadRight,
        LeftStickUp, LeftStickDown, LeftStickLeft, LeftStickRight,
        RightStickUp, RightStickDown, RightStickLeft, RightStickRight,
        Start, Back,
        LeftShoulder, RightShoulder,
        LeftTrigger, RightTrigger
    }

    public class GamepadButtonEventArgs : EventArgs
    {
        public GamepadButton Button { get; private set; }
        public GamepadButtonEventArgs(GamepadButton button)
        {
            Button = button;
        }
    }

    public class GamepadService : IDisposable
    {
        private Controller _controller;
        private Thread _pollThread;
        private volatile bool _isRunning;
        private volatile bool _disposed;

        private State _previousState;
        private bool _previousConnected;

        private const float StickDeadZone = 0.5f;
        private const int PollIntervalMs = 100;

        private bool _stickUp, _stickDown, _stickLeft, _stickRight;
        private bool _rightStickUp, _rightStickDown, _rightStickLeft, _rightStickRight;

        public event EventHandler<GamepadButtonEventArgs> ButtonPressed;
        public event EventHandler<GamepadButtonEventArgs> ButtonReleased;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        public bool IsConnected
        {
            get
            {
                try { return _controller != null && _controller.IsConnected; }
                catch { return false; }
            }
        }

        public bool IsRunning { get { return _isRunning; } }

        public GamepadService(bool autoStart = true)
        {
            if (autoStart)
            {
                Start();
            }
        }

        public void Start()
        {
            if (_isRunning || _disposed) return;

            _isRunning = true;

            // Find connected controller
            _controller = null;
            for (int i = 0; i < 4; i++)
            {
                var testController = new Controller((UserIndex)i);
                try
                {
                    if (testController.IsConnected)
                    {
                        _controller = testController;
                        break;
                    }
                }
                catch { }
            }

            if (_controller == null)
            {
                _controller = new Controller(UserIndex.One);
            }

            _previousState = new State();
            _previousConnected = false;
            _stickUp = _stickDown = _stickLeft = _stickRight = false;
            _rightStickUp = _rightStickDown = _rightStickLeft = _rightStickRight = false;

            _pollThread = new Thread(PollLoop)
            {
                IsBackground = true,
                Name = "GamepadPolling"
            };
            _pollThread.Start();
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            if (_pollThread != null && _pollThread.IsAlive)
            {
                _pollThread.Join(1000);
            }
            _pollThread = null;
        }

        private void PollLoop()
        {
            while (_isRunning && !_disposed)
            {
                try
                {
                    bool isConnected = false;
                    try { isConnected = _controller.IsConnected; }
                    catch { isConnected = false; }

                    if (isConnected && !_previousConnected)
                    {
                        Connected?.Invoke(this, EventArgs.Empty);
                        _previousState = new State();
                    }
                    else if (!isConnected && _previousConnected)
                    {
                        Disconnected?.Invoke(this, EventArgs.Empty);
                    }

                    _previousConnected = isConnected;

                    if (isConnected)
                    {
                        State state;
                        try { state = _controller.GetState(); }
                        catch { Thread.Sleep(PollIntervalMs); continue; }

                        if (state.PacketNumber != _previousState.PacketNumber)
                        {
                            ProcessState(state, _previousState);
                            _previousState = state;
                        }
                    }

                    Thread.Sleep(PollIntervalMs);
                }
                catch
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void ProcessState(State current, State previous)
        {
            var gp = current.Gamepad;
            var prev = previous.Gamepad;

            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.A, GamepadButton.A);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.B, GamepadButton.B);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.X, GamepadButton.X);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.Y, GamepadButton.Y);

            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.DPadUp, GamepadButton.DPadUp);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.DPadDown, GamepadButton.DPadDown);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.DPadLeft, GamepadButton.DPadLeft);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.DPadRight, GamepadButton.DPadRight);

            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.LeftShoulder, GamepadButton.LeftShoulder);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.RightShoulder, GamepadButton.RightShoulder);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.Start, GamepadButton.Start);
            CheckButton(gp.Buttons, prev.Buttons, GamepadButtonFlags.Back, GamepadButton.Back);

            ProcessLeftStick(gp.LeftThumbX, gp.LeftThumbY);
            ProcessRightStick(gp.RightThumbX, gp.RightThumbY);

            ProcessTriggers(gp.LeftTrigger, _previousState.Gamepad.LeftTrigger, GamepadButton.LeftTrigger);
            ProcessTriggers(gp.RightTrigger, _previousState.Gamepad.RightTrigger, GamepadButton.RightTrigger);
        }

        private void CheckButton(GamepadButtonFlags current, GamepadButtonFlags previous,
            GamepadButtonFlags flag, GamepadButton button)
        {
            bool isPressed = (current & flag) == flag;
            bool wasPressed = (previous & flag) == flag;

            if (isPressed && !wasPressed)
                ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(button));
            else if (!isPressed && wasPressed)
                ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(button));
        }

        private void ProcessLeftStick(short x, short y)
        {
            float nx = x / 32768f;
            float ny = y / 32768f;

            bool newUp = ny > StickDeadZone;
            bool newDown = ny < -StickDeadZone;
            bool newLeft = nx < -StickDeadZone;
            bool newRight = nx > StickDeadZone;

            if (newUp && !_stickUp) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickUp));
            else if (!newUp && _stickUp) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickUp));

            if (newDown && !_stickDown) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickDown));
            else if (!newDown && _stickDown) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickDown));

            if (newLeft && !_stickLeft) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickLeft));
            else if (!newLeft && _stickLeft) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickLeft));

            if (newRight && !_stickRight) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickRight));
            else if (!newRight && _stickRight) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.LeftStickRight));

            _stickUp = newUp;
            _stickDown = newDown;
            _stickLeft = newLeft;
            _stickRight = newRight;
        }

        private void ProcessRightStick(short x, short y)
        {
            float nx = x / 32768f;
            float ny = y / 32768f;

            bool newUp = ny > StickDeadZone;
            bool newDown = ny < -StickDeadZone;
            bool newLeft = nx < -StickDeadZone;
            bool newRight = nx > StickDeadZone;

            if (newUp && !_rightStickUp) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickUp));
            else if (!newUp && _rightStickUp) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickUp));

            if (newDown && !_rightStickDown) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickDown));
            else if (!newDown && _rightStickDown) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickDown));

            if (newLeft && !_rightStickLeft) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickLeft));
            else if (!newLeft && _rightStickLeft) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickLeft));

            if (newRight && !_rightStickRight) ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickRight));
            else if (!newRight && _rightStickRight) ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(GamepadButton.RightStickRight));

            _rightStickUp = newUp;
            _rightStickDown = newDown;
            _rightStickLeft = newLeft;
            _rightStickRight = newRight;
        }

        private void ProcessTriggers(byte current, byte previous, GamepadButton button)
        {
            const byte triggerThreshold = 128; // 50% pressed
            bool currentPressed = current > triggerThreshold;
            bool previousPressed = previous > triggerThreshold;

            if (currentPressed && !previousPressed)
                ButtonPressed?.Invoke(this, new GamepadButtonEventArgs(button));
            else if (!currentPressed && previousPressed)
                ButtonReleased?.Invoke(this, new GamepadButtonEventArgs(button));
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}