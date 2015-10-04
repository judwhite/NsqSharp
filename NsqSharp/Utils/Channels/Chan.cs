using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NsqSharp.Utils.Channels
{
    /// <summary>
    /// Channel for synchronizing communication between threads. Supports foreach to read from the channel until it's closed. See also <see cref="Select"/>.
    /// </summary>
    /// <typeparam name="T">The message type communicated over the channel.</typeparam>
    public sealed class Chan<T> : IReceiveOnlyChan<T>, ISendOnlyChan<T>, IEnumerable<T>
    {
        private readonly object _sendLocker = new object();
        private readonly object _receiveLocker = new object();
        private readonly object _isClosedLocker = new object();

        private readonly AutoResetEvent _readyToReceive = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent _sent = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent _receiveComplete = new AutoResetEvent(initialState: false);

        private readonly List<AutoResetEvent> _listenForSend = new List<AutoResetEvent>();
        private readonly List<AutoResetEvent> _listenForReceive = new List<AutoResetEvent>();
        private readonly object _listenForSendLocker = new object();
        private readonly object _listenForReceiveLocker = new object();

        private static readonly TimeSpan _infiniteTimeSpan = TimeSpan.FromMilliseconds(-1);

        private bool _isReadyToSend;
        private bool _isClosed;
        private bool _isDrained;

        private readonly int _bufferSize;
        private readonly Queue<T> _buffer;
        private readonly object _bufferLocker = new object();

        /// <summary>
        /// Initializes a new unbuffered channel.
        /// </summary>
        public Chan()
        {
            _bufferSize = 0;
            _buffer = new Queue<T>(1);
        }

        /// <summary>
        /// Initializes a new channel with specified <paramref name="bufferSize"/>.
        /// </summary>
        /// <param name="bufferSize">The size of the send buffer.</param>
        public Chan(int bufferSize)
        {
            _bufferSize = bufferSize;
            _buffer = new Queue<T>(_bufferSize + 1);
        }

        /// <summary>
        /// Sends a message to the channel. Blocks until the message is received.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(T message)
        {
            ((ISendOnlyChan)this).TrySend(message, _infiniteTimeSpan);
        }

        bool ISendOnlyChan.TrySend(object message, TimeSpan timeout)
        {
            if (_isClosed)
                throw new ChannelClosedException();

            lock (_sendLocker)
            {
                if (_bufferSize != 0)
                {
                    lock (_bufferLocker)
                    {
                        if (_buffer.Count < _bufferSize)
                        {
                            if (timeout == _infiniteTimeSpan)
                                timeout = TimeSpan.FromMilliseconds(20);
                        }
                    }
                }

                _isReadyToSend = true;

                PumpListenForSend();

                bool success = _readyToReceive.WaitOne(timeout.Milliseconds);
                if (_isClosed)
                {
                    _isReadyToSend = false;
                    throw new ChannelClosedException();
                }
                bool waitForReceive = true;
                if (!success)
                {
                    lock (_bufferLocker)
                    {
                        if (_buffer.Count == _bufferSize)
                        {
                            _isReadyToSend = false;
                            return false;
                        }
                        else
                        {
                            waitForReceive = false;
                        }
                    }
                }

                Enqueue((T)message);

                if (waitForReceive)
                {
                    _sent.Set();
                    _receiveComplete.WaitOne();
                }

                return true;
            }
        }

        private void Enqueue(T value)
        {
            lock (_bufferLocker)
            {
                _buffer.Enqueue(value);
            }
        }

        private T Dequeue()
        {
            lock (_bufferLocker)
            {
                return _buffer.Dequeue();
            }
        }

        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready or the channel is closed.
        /// </summary>
        /// <returns>The message received; or default(T) if the channel was closed.</returns>
        public T Receive()
        {
            bool ok;
            var value = ReceiveOk(out ok);
            return value;
        }

        object IReceiveOnlyChan.ReceiveOk(out bool ok)
        {
            return ReceiveOk(out ok);
        }

        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready or the channel is closed.
        /// </summary>
        /// <returns>The message received; or default(T) if the channel was closed. Includes an indicator
        /// whether the channel was closed or not.</returns>
        public T ReceiveOk(out bool ok)
        {
            T data;
            lock (_receiveLocker)
            {
                lock (_bufferLocker)
                {
                    if (_buffer.Count > 0)
                    {
                        ok = true;
                        data = Dequeue();
                        _isReadyToSend = (_buffer.Count > 0);
                        return data;
                    }
                }

                if (_isDrained)
                {
                    ok = false;
                    return default(T);
                }

                _readyToReceive.Set();

                PumpListenForReceive();

                int waitForSendTimeout = _isClosed ? 30 : -1;

                _sent.WaitOne(waitForSendTimeout);

                lock (_bufferLocker)
                {
                    if (_buffer.Count == 0 && _isClosed)
                    {
                        ok = false;
                        data = default(T);
                        _isReadyToSend = false;
                        _isDrained = true;
                    }
                    else
                    {
                        ok = true;
                        data = Dequeue();
                        _isReadyToSend = (_buffer.Count > 0);
                    }
                }
                _receiveComplete.Set();
            }

            return data;
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate
        /// through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            while (true)
            {
                bool ok;
                var value = ReceiveOk(out ok);

                if (!ok)
                    yield break;
                else
                    yield return value;
            }
        }

        /// <summary>
        /// Closes the channel.
        /// </summary>
        public void Close()
        {
            lock (_isClosedLocker)
            {
                if (_isClosed)
                {
                    return;
                }

                _isClosed = true;

                PumpListenForSend();
                PumpListenForReceive();

                _sent.Set();
                _readyToReceive.Set();
            }
        }

        private void PumpListenForSend()
        {
            lock (_listenForSendLocker)
            {
                foreach (var listener in _listenForSend)
                {
                    listener.Set();
                }
            }
        }

        private void PumpListenForReceive()
        {
            lock (_listenForReceiveLocker)
            {
                foreach (var listener in _listenForReceive)
                {
                    listener.Set();
                }
            }
        }

        bool IReceiveOnlyChan.TryLockReceive()
        {
            return Monitor.TryEnter(_receiveLocker);
        }

        bool ISendOnlyChan.TryLockSend()
        {
            return Monitor.TryEnter(_sendLocker);
        }

        void IReceiveOnlyChan.UnlockReceive()
        {
            Monitor.Exit(_receiveLocker);
        }

        void ISendOnlyChan.UnlockSend()
        {
            Monitor.Exit(_sendLocker);
        }

        bool IReceiveOnlyChan.IsReadyToSend
        {
            get { return _isReadyToSend || _isClosed; }
        }

        bool IChan.IsClosed
        {
            get { return _isClosed; }
        }

        void IChan.AddListenForSend(AutoResetEvent func)
        {
            lock (_listenForSendLocker)
            {
                _listenForSend.Add(func);
            }
        }

        void IChan.AddListenForReceive(AutoResetEvent func)
        {
            lock (_listenForReceiveLocker)
            {
                _listenForReceive.Add(func);
            }
        }

        void IChan.RemoveListenForSend(AutoResetEvent autoResetEvent)
        {
            lock (_listenForSendLocker)
            {
                _listenForSend.Remove(autoResetEvent);
            }
        }

        void IChan.RemoveListenForReceive(AutoResetEvent autoResetEvent)
        {
            lock (_listenForReceiveLocker)
            {
                _listenForReceive.Remove(autoResetEvent);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
