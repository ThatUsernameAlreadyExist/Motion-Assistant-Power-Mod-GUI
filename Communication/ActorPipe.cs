using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Threading;


namespace BidirectionalPipe.ActorModel
{
    /// <summary>
    /// Actor Model Bidirectional Pipe Communication
    /// Two unidirectional pipes with internal thread isolation
    /// Lock-free communication via atomic flags and concurrent queues
    /// Supports .NET Framework 4.8
    /// </summary>
    public class ActorPipe : IDisposable
    {
        #region Enums and Data Structures

        /// <summary>
        /// Status of the pipe connection (accessed via lock-free flags)
        /// </summary>
        public enum PipeStatus
        {
            Disconnected,
            Connecting,
            Connected,
            Error
        }


        /// <summary>
        /// Generic base command class for serialization
        /// </summary>
        [Serializable]
        public abstract class CommandBase
        {
            public string CommandId { get; set; }

            protected CommandBase()
            {
                CommandId = Guid.NewGuid().ToString();
            }

            protected CommandBase(string commandId)
            {
                CommandId = commandId ?? Guid.NewGuid().ToString();
            }
        }


        /// <summary>
        /// String command
        /// </summary>
        [Serializable]
        public class StringCommand : CommandBase
        {
            public string Data { get; set; }

            public StringCommand(string data, string commandId) : base(commandId)
            {
                Data = data;
            }
        }


        /// <summary>
        /// Integer command
        /// </summary>
        [Serializable]
        public class IntCommand : CommandBase
        {
            public int Data { get; set; }

            public IntCommand(int data, string commandId) : base(commandId)
            {
                Data = data;
            }
        }


        /// <summary>
        /// Unsigned integer command
        /// </summary>
        [Serializable]
        public class UintCommand : CommandBase
        {
            public uint Data { get; set; }

            public UintCommand(uint data, string commandId) : base(commandId)
            {
                Data = data;
            }
        }


        /// <summary>
        /// Float command
        /// </summary>
        [Serializable]
        public class FloatCommand : CommandBase
        {
            public float Data { get; set; }

            public FloatCommand(float data, string commandId) : base(commandId)
            {
                Data = data;
            }
        }


        /// <summary>
        /// Generic List command - supports List&lt;T&gt; for any type T
        /// </summary>
        [Serializable]
        public class ListCommand<T> : CommandBase
        {
            public List<T> Data { get; set; }


            public ListCommand(List<T> data, string commandId) : base(commandId)
            {
                Data = data ?? new List<T>();
            }
        }


        /// <summary>
        /// Backward compatibility - string list command
        /// </summary>
        [Serializable]
        public class StringListCommand : ListCommand<string>
        {
            public StringListCommand(List<string> data, string commandId) : base(data, commandId) { }
        }


        /// <summary>
        /// Integer list command
        /// </summary>
        [Serializable]
        public class IntListCommand : ListCommand<int>
        {
            public IntListCommand(List<int> data, string commandId) : base(data, commandId) { }
        }


        /// <summary>
        /// Container command with mixed data types
        /// </summary>
        [Serializable]
        public class ContainerCommand : CommandBase
        {
            public Dictionary<string, object> Data { get; set; }

            public ContainerCommand(Dictionary<string, object> data, string commandId) : base(commandId)
            {
                Data = data;
            }
        }


        /// <summary>
        /// Event arguments for received commands
        /// </summary>
        public class CommandReceivedEventArgs : EventArgs
        {
            public CommandBase Command { get; set; }

            public CommandReceivedEventArgs(CommandBase command)
            {
                Command = command;
            }
        }


        /// <summary>
        /// Event arguments for status changes
        /// </summary>
        public class StatusChangedEventArgs : EventArgs
        {
            public PipeStatus OldStatus { get; set; }
            public PipeStatus NewStatus { get; set; }
            public string Message { get; set; }


            public StatusChangedEventArgs(PipeStatus oldStatus, PipeStatus newStatus, string message = null)
            {
                OldStatus = oldStatus;
                NewStatus = newStatus;
                Message = message;
            }
        }

        #endregion

        #region Private Fields - Lock-free State Communication

        // Read Pipe Actor State (only accessed by internal read thread)
        private PipeForReading _readPipe;
        private readonly string _readPipeName;

        // Write Pipe Actor State (only accessed by internal write thread)
        private PipeForWriting _writePipe;
        private readonly string _writePipeName;

        // Lock-free state flags (accessed by external threads)
        private volatile PipeStatus _status = PipeStatus.Disconnected;
        private volatile bool _isRunning = false;
        private volatile bool _isConnected = false;
        private volatile int _readPipeConnected = 0;  // 0 = not connected, 1 = connected
        private volatile int _writePipeConnected = 0; // 0 = not connected, 1 = connected

        // Configuration
        private readonly int _connectionTimeoutMs;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when a command is received from the pipe
        /// </summary>
        public event EventHandler<CommandReceivedEventArgs> CommandReceived;

        /// <summary>
        /// Event fired when pipe status changes
        /// </summary>
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        #endregion

        #region Properties - Lock-free Access

        /// <summary>
        /// Current pipe status (lock-free read)
        /// </summary>
        public PipeStatus Status => _status;

        /// <summary>
        /// Is the pipe currently running (lock-free read)
        /// </summary>
        public bool IsRunning => _isRunning && _isConnected;

        /// <summary>
        /// Is the connection established (lock-free read)
        /// </summary>
        public bool IsConnected => _isConnected;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for Slave role
        /// </summary>
        /// <param name="basePipeName">Base name for the pipes (must match master's basePipeName)</param>
        /// <param name="connectionTimeoutMs">Connection timeout in milliseconds (default: 30000 = 30 seconds)</param>
        public ActorPipe(string basePipeName, int connectionTimeoutMs = 30000)
        {
            _connectionTimeoutMs = connectionTimeoutMs;

            // Create separate pipe names for read and write directions (must match master!)
            // Slave reads FROM MasterToSlave pipe and writes TO SlaveToMaster pipe
            _readPipeName = $"Global\\{basePipeName}_MasterToSlave";
            _writePipeName = $"Global\\{basePipeName}_SlaveToMaster";
        }

        #endregion

        #region Public Methods - Message Interface (No Direct Pipe Access)

        /// <summary>
        /// Start the actor pipe communication
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            UpdateStatus(PipeStatus.Connecting);

            try
            {
                // Create read and write pipe actors
                _readPipe = new PipeForReading(_readPipeName, OnReadPipeStatusChanged, OnCommandReceived, _connectionTimeoutMs);
                _writePipe = new PipeForWriting(_writePipeName, OnWritePipeStatusChanged, _connectionTimeoutMs);

                _writePipe.Start();
                _readPipe.Start();
            }
            catch (Exception ex)
            {
                UpdateStatus(PipeStatus.Error, $"Failed to start pipe: {ex.Message}");
                _isRunning = false;
                throw;
            }
        }


        /// <summary>
        /// Stop the actor pipe communication
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            try
            {
                // Stop both pipe actors
                _readPipe?.Stop();
                _writePipe?.Stop();
            }
            catch (Exception)
            {
                // Log error but don't throw to allow proper cleanup
            }
            finally
            {
                UpdateStatus(PipeStatus.Disconnected);
            }
        }


        /// <summary>
        /// Send a string command (non-blocking)
        /// </summary>
        public void SendString(string data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new StringCommand(data, commandId));
        }


        /// <summary>
        /// Send an integer command (non-blocking)
        /// </summary>
        public void SendInt(int data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new IntCommand(data, commandId));
        }


        /// <summary>
        /// Send an unsigned integer command (non-blocking)
        /// </summary>
        public void SendUint(uint data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new UintCommand(data, commandId));
        }


        /// <summary>
        /// Send a float command (non-blocking)
        /// </summary>
        public void SendFloat(float data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new FloatCommand(data, commandId));
        }


        /// <summary>
        /// Send a generic list command (non-blocking)
        /// </summary>
        public void SendList<T>(List<T> data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new ListCommand<T>(data, commandId));
        }


        /// <summary>
        /// Send a string list command (non-blocking)
        /// </summary>
        public void SendStringList(List<string> data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new StringListCommand(data, commandId));
        }


        /// <summary>
        /// Send an integer list command (non-blocking)
        /// </summary>
        public void SendIntList(List<int> data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new IntListCommand(data, commandId));
        }


        /// <summary>
        /// Send a container command (non-blocking)
        /// </summary>
        public void SendContainer(Dictionary<string, object> data, string commandId = null)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            _writePipe?.SendCommand(new ContainerCommand(data, commandId));
        }


        /// <summary>
        /// Send a custom command (non-blocking)
        /// </summary>
        public void SendCommand(CommandBase command)
        {
            if (!_isRunning) throw new InvalidOperationException("Pipe is not running");
            if (command == null) throw new ArgumentNullException(nameof(command));

            _writePipe?.SendCommand(command);
        }

        #endregion

        #region Private Methods - Internal Actor Implementation

        private void OnReadPipeStatusChanged(PipeStatus newStatus, string message)
        {
            var oldValue = Interlocked.Exchange(ref _readPipeConnected, newStatus == PipeStatus.Connected ? 1 : 0);

            // Update overall connection status
            UpdateOverallConnectionStatus();

            // Fire status changed event for errors
            if (newStatus == PipeStatus.Error || newStatus == PipeStatus.Disconnected)
            {
                var oldStatus = _status;
                _status = newStatus;
                FireStatusChangedEvent(oldStatus, newStatus, $"Read pipe: {message}");
            }
        }


        private void OnWritePipeStatusChanged(PipeStatus newStatus, string message)
        {
            var oldValue = Interlocked.Exchange(ref _writePipeConnected, newStatus == PipeStatus.Connected ? 1 : 0);

            // Update overall connection status
            UpdateOverallConnectionStatus();

            // Fire status changed event for errors
            if (newStatus == PipeStatus.Error || newStatus == PipeStatus.Disconnected)
            {
                var oldStatus = _status;
                _status = newStatus;
                FireStatusChangedEvent(oldStatus, newStatus, $"Write pipe: {message}");
            }
        }


        private void UpdateOverallConnectionStatus()
        {
            // Both pipes must be connected for overall connection
            bool bothConnected = (_readPipeConnected == 1) && (_writePipeConnected == 1);

            var oldStatus = _status;
            if (bothConnected && !_isConnected)
            {
                _isConnected = true;
                _status = PipeStatus.Connected;
                FireStatusChangedEvent(oldStatus, PipeStatus.Connected, "Both pipes connected");
            }
            else if (!bothConnected && _isConnected)
            {
                _isConnected = false;
                if (_status != PipeStatus.Error)
                {
                    _status = PipeStatus.Connecting;
                }
            }
        }


        private void OnCommandReceived(CommandBase command)
        {
            if (command != null)
            {
                FireCommandReceivedEvent(command);
            }
        }


        private void UpdateStatus(PipeStatus newStatus, string message = null)
        {
            var oldStatus = _status;
            _status = newStatus;

            if (newStatus == PipeStatus.Connected)
            {
                _isConnected = true;
            }
            else if (newStatus == PipeStatus.Error || newStatus == PipeStatus.Disconnected)
            {
                _isConnected = false;
            }

            if (oldStatus != newStatus)
            {
                FireStatusChangedEvent(oldStatus, newStatus, message);
            }
        }


        private void FireCommandReceivedEvent(CommandBase command)
        {
            try
            {
                // Use Avalonia's dispatcher to ensure UI thread execution
                if (Dispatcher.UIThread.CheckAccess())
                {
                    // We're already on the UI thread, invoke directly
                    CommandReceived?.Invoke(this, new CommandReceivedEventArgs(command));
                }
                else
                {
                    // We're on a background thread, marshal to UI thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            CommandReceived?.Invoke(this, new CommandReceivedEventArgs(command));
                        }
                        catch (Exception)
                        {
                            // Suppress exceptions in event handlers to prevent crashing the pipe
                        }
                    }, DispatcherPriority.Normal);
                }
            }
            catch (Exception)
            {
                // Suppress exceptions to prevent pipe failures from event handler issues
            }
        }


        private void FireStatusChangedEvent(PipeStatus oldStatus, PipeStatus newStatus, string message)
        {
            if (oldStatus == newStatus && string.IsNullOrEmpty(message))
                return;

            try
            {
                var syncContext = SynchronizationContext.Current;

                if (syncContext != null)
                {
                    syncContext.Post(_ =>
                    {
                        try
                        {
                            StatusChanged?.Invoke(this, new StatusChangedEventArgs(oldStatus, newStatus, message));
                        }
                        catch (Exception)
                        {
                            // Suppress exceptions in event handlers
                        }
                    }, null);
                }
                else
                {
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(oldStatus, newStatus, message));
                }
            }
            catch (Exception)
            {
                // Suppress exceptions to prevent pipe failures from event handler issues
            }
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed = false;


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                }
                _disposed = true;
            }
        }


        ~ActorPipe()
        {
            Dispose(false);
        }

        #endregion
    }
}
