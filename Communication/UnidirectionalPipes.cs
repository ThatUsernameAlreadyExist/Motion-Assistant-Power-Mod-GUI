using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BidirectionalPipe.ActorModel.ActorPipe;

namespace BidirectionalPipe.ActorModel
{
    /// <summary>
    /// Unidirectional Pipe Actor for Reading Operations
    /// All pipe I/O operations happen in internal thread
    /// Lock-free communication with external threads
    /// </summary>
    internal class PipeForReading
    {
        #region Private Fields

        private readonly string _pipeName;
        private readonly Action<ActorPipe.PipeStatus, string> _statusCallback;
        private readonly Action<ActorPipe.CommandBase> _commandCallback;
        private readonly int _connectionTimeoutMs;

        private volatile bool _isRunning = false;
        private volatile bool _shouldStop = false;

        private NamedPipeClientStream _clientPipe;
        private Thread _readThread;

        private ZeroCopyCommandSerializer _deserializer = new ZeroCopyCommandSerializer();
        [ThreadStatic]
        private static StringBuilder t_messageBuilder;

        #endregion

        #region Constructor

        public PipeForReading(string pipeName,
                             Action<ActorPipe.PipeStatus, string> statusCallback,
                             Action<ActorPipe.CommandBase> commandCallback,
                             int connectionTimeoutMs = 30000)
        {
            _pipeName = pipeName;
            _statusCallback = statusCallback;
            _commandCallback = commandCallback;
            _connectionTimeoutMs = connectionTimeoutMs;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (_isRunning)
                return;

            _shouldStop = false;
            _isRunning = true;

            _readThread = new Thread(ReadThreadProc);
            _readThread.IsBackground = true;
            _readThread.Start();
        }


        public void Stop()
        {
            _shouldStop = true;
            _isRunning = false;

            try
            {
                // Wait for read thread to stop gracefully
                if (_readThread != null && _readThread.IsAlive)
                {
                    if (!_readThread.Join(3000))
                    {
                        _readThread.Abort();
                    }
                }

                // Close pipes (will interrupt blocking reads)
                _clientPipe?.Close();
            }
            catch (Exception)
            {
                // Suppress exceptions during cleanup to ensure proper shutdown
            }
            finally
            {
                UpdateStatus(PipeStatus.Disconnected);
            }
        }

        #endregion

        #region Private Methods

        private void ReadThreadProc()
        {
            try
            {
                UpdateStatus(PipeStatus.Connecting);
                ConnectAsSlave();
            }
            catch (Exception ex)
            {
                UpdateStatus(PipeStatus.Error, ex.Message);
            }
        }

        private void ConnectAsSlave()
        {
            try
            {
                // Give master time to create server pipes
                Thread.Sleep(200);

                // Slave creates CLIENT pipe to READ from master (MasterToSlave pipe)
                _clientPipe = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.In,
                    PipeOptions.Asynchronous);

                _clientPipe.Connect(_connectionTimeoutMs);

                if (_clientPipe.IsConnected)
                {
                    UpdateStatus(PipeStatus.Connected);
                    HandleReading();
                }
            }
            catch (TimeoutException)
            {
                UpdateStatus(PipeStatus.Error, $"Slave read connection timeout ({_connectionTimeoutMs / 1000}s)");
            }
            catch (Exception ex)
            {
                UpdateStatus(PipeStatus.Error, $"Slave read pipe error: {ex.Message}");
            }
        }


        private void HandleReading()
        {
            byte[] buffer = new byte[4096];

            // Reuse StringBuilder across calls
            if (t_messageBuilder == null)
                t_messageBuilder = new StringBuilder(8192);
            else
                t_messageBuilder.Clear();

            StringBuilder messageBuilder = t_messageBuilder;
            Task<int> pendingReadTask = null;

            while (_isRunning && !_shouldStop)
            {
                try
                {
                    PipeStream pipe = GetPipe();
                    if (pipe == null || !pipe.IsConnected)
                    {
                        break;
                    }

                    if (pendingReadTask == null)
                    {
                        pendingReadTask = Task<int>.Factory.FromAsync(
                            pipe.BeginRead,
                            pipe.EndRead,
                            buffer,
                            0,
                            buffer.Length,
                            null);
                    }

                    if (pendingReadTask.Wait(1000))
                    {
                        int bytesRead = 0;

                        try
                        {
                            bytesRead = pendingReadTask.Result;
                        }
                        catch (AggregateException aggEx)
                        {
                            var innerEx = aggEx.InnerException ?? aggEx;
                            if (innerEx is IOException)
                            {
                                break;
                            }
                            throw;
                        }
                        finally
                        {
                            pendingReadTask = null;
                        }

                        if (bytesRead > 0)
                        {
                            // Decode UTF8 bytes and append
                            string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            messageBuilder.Append(chunk);

                            // Process complete messages (ending with newline)
                            ProcessCompleteMessages(messageBuilder);
                        }
                        else if (bytesRead == 0)
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            UpdateStatus(PipeStatus.Disconnected);
        }


        // Optimized message processing to avoid string allocations
        private void ProcessCompleteMessages(StringBuilder messageBuilder)
        {
            int startIndex = 0;
            int length = messageBuilder.Length;

            for (int i = 0; i < length; i++)
            {
                if (messageBuilder[i] == '\n')
                {
                    int messageLength = i - startIndex;

                    if (messageLength > 0)
                    {
                        // Extract message without creating intermediate string until necessary
                        string message = messageBuilder.ToString(startIndex, messageLength);

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            ProcessReceivedCommand(message);
                        }
                    }

                    startIndex = i + 1;
                }
            }

            // Remove processed messages, keep remaining partial message
            if (startIndex > 0)
            {
                messageBuilder.Remove(0, startIndex);
            }
        }


        private PipeStream GetPipe()
        {
            return (PipeStream)_clientPipe;
        }


        private void ProcessReceivedCommand(string serializedCommand)
        {
            try
            {
                var command = DeserializeCommand(serializedCommand);
                if (command != null)
                {
                    _commandCallback?.Invoke(command);
                }
            }
            catch (Exception)
            {
                // Suppress command processing errors to prevent pipe failure
            }
        }


        private ActorPipe.CommandBase DeserializeCommand(string serializedCommand)
        {
            try
            {
                if (string.IsNullOrEmpty(serializedCommand))
                    return null;

                byte[] binaryData = Convert.FromBase64String(serializedCommand);
                return _deserializer.DeserializeCommand(binaryData);
            }
            catch (FormatException)
            {
                // Invalid Base64 data - return null to skip this message
                return null;
            }
            catch (Exception)
            {
                // Deserialization failed - return null to skip this message
                return null;
            }
        }


        private void UpdateStatus(ActorPipe.PipeStatus newStatus, string message = null)
        {
            _statusCallback?.Invoke(newStatus, message);
        }

        #endregion
    }


    /// <summary>
    /// Unidirectional Pipe Actor for Writing Operations
    /// All pipe I/O operations happen in internal thread
    /// Lock-free communication with external threads
    /// </summary>
    internal class PipeForWriting
    {
        #region Private Fields

        private readonly string _pipeName;
        private readonly Action<ActorPipe.PipeStatus, string> _statusCallback;
        private readonly int _connectionTimeoutMs;

        private volatile bool _isRunning = false;
        private volatile bool _shouldStop = false;

        private NamedPipeClientStream _clientPipe;
        private Thread _writeThread;
        private readonly ConcurrentQueue<ActorPipe.CommandBase> _sendQueue = new ConcurrentQueue<ActorPipe.CommandBase>();
        private readonly AutoResetEvent _sendEvent = new AutoResetEvent(false);

        private readonly ZeroCopyCommandSerializer _serializer = new ZeroCopyCommandSerializer();
        private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

        #endregion

        #region Constructor

        public PipeForWriting(string pipeName,
                             Action<ActorPipe.PipeStatus, string> statusCallback,
                             int connectionTimeoutMs = 30000)
        {
            _pipeName = pipeName;
            _statusCallback = statusCallback;
            _connectionTimeoutMs = connectionTimeoutMs;
        }

        #endregion

        #region Public Properties


        #endregion

        #region Public Methods

        public void Start()
        {
            if (_isRunning)
                return;

            _shouldStop = false;
            _isRunning = true;

            _writeThread = new Thread(WriteThreadProc);
            _writeThread.IsBackground = true;
            _writeThread.Start();
        }


        public void Stop()
        {
            _shouldStop = true;
            _isRunning = false;

            try
            {
                // Signal write thread to stop
                _sendEvent.Set();

                // Wait for write thread to stop gracefully
                if (_writeThread != null && _writeThread.IsAlive)
                {
                    if (!_writeThread.Join(3000))
                    {
                        _writeThread.Abort();
                    }
                }

                // Close pipes (will interrupt blocking operations)
                _clientPipe?.Close();

                // Clear send queue
                while (_sendQueue.TryDequeue(out _)) { }
            }
            catch (Exception)
            {
                // Suppress exceptions during cleanup to ensure proper shutdown
            }
            finally
            {
                UpdateStatus(PipeStatus.Disconnected);
            }
        }


        /// <summary>
        /// Send command (non-blocking - adds to queue)
        /// </summary>
        public void SendCommand(ActorPipe.CommandBase command)
        {
            if (command == null) return;

            _sendQueue.Enqueue(command);
            _sendEvent.Set();
        }

        #endregion

        #region Private Methods


        private void WriteThreadProc()
        {
            try
            {
                UpdateStatus(PipeStatus.Connecting);
                ConnectAsSlave();
            }
            catch (Exception ex)
            {
                UpdateStatus(PipeStatus.Error, ex.Message);
            }
        }


        private void ConnectAsSlave()
        {
            try
            {
                // Give master time to create server pipes
                Thread.Sleep(200);

                // Slave creates CLIENT pipe to WRITE to master (SlaveToMaster pipe)
                _clientPipe = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);

                _clientPipe.Connect(_connectionTimeoutMs);

                if (_clientPipe.IsConnected)
                {
                    UpdateStatus(PipeStatus.Connected);
                    ProcessSendQueue();
                }
            }
            catch (TimeoutException)
            {
                UpdateStatus(PipeStatus.Error, $"Slave write connection timeout ({_connectionTimeoutMs / 1000}s)");
            }
            catch (Exception ex)
            {
                UpdateStatus(PipeStatus.Error, $"Slave write pipe error: {ex.Message}");
            }
        }


        private void ProcessSendQueue()
        {
            while (_isRunning && !_shouldStop)
            {
                try
                {
                    PipeStream pipe = GetPipe();
                    if (pipe == null || !pipe.IsConnected)
                        break;

                    if (_sendQueue.TryDequeue(out ActorPipe.CommandBase command))
                    {
                        byte[] rentedBuffer = null;
                        try
                        {
                            string serializedCommand = SerializeCommand(command);

                            int maxByteCount = Encoding.UTF8.GetMaxByteCount(serializedCommand.Length);
                            rentedBuffer = _bytePool.Rent(maxByteCount);

                            int actualByteCount = Encoding.UTF8.GetBytes(serializedCommand, 0,
                                                                          serializedCommand.Length,
                                                                          rentedBuffer, 0);

                            pipe.Write(rentedBuffer, 0, actualByteCount);
                            pipe.Flush();
                        }
                        catch (IOException)
                        {
                            // Pipe connection lost - exit send loop
                            break;
                        }
                        finally
                        {
                            if (rentedBuffer != null)
                                _bytePool.Return(rentedBuffer);
                        }
                    }
                    else
                    {
                        _sendEvent.WaitOne(1000);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            UpdateStatus(PipeStatus.Disconnected);
        }


        private PipeStream GetPipe()
        {
            return (PipeStream)_clientPipe;
        }


        private string SerializeCommand(ActorPipe.CommandBase command)
        {
            try
            {
                byte[] binaryData = _serializer.SerializeCommand(command);

                // Convert to Base64 and add newline delimiter
                string base64 = Convert.ToBase64String(binaryData);
                return base64 + "\n";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize command: {ex.Message}", ex);
            }
        }


        private void UpdateStatus(ActorPipe.PipeStatus newStatus, string message = null)
        {
            _statusCallback?.Invoke(newStatus, message);
        }

        #endregion
    }
}
