using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NsqSharp.Channels
{
    /// <summary>
    /// Channel for synchronizing communication between threads. Supports foreach to read from the channel until it's closed. See also <see cref="Select"/>.
    /// </summary>
    /// <typeparam name="T">The message type communicated over the channel.</typeparam>
    public class Chan<T> : IChan, IEnumerable<T>
    {
        private readonly object _sendLocker = new object();
        private readonly object _receiveLocker = new object();
        private readonly object _isClosedLocker = new object();

        private readonly AutoResetEvent _readyToReceive = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent _sent = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent _receiveComplete = new AutoResetEvent(initialState: false);

        private readonly List<AutoResetEvent> _listeners = new List<AutoResetEvent>();

        private T _data;

        private bool _isReadyToSend;
        private bool _isReadyToReceive;
        private bool _isClosed;

        /// <summary>
        /// Sends a message to the channel. Blocks until the message is received.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(T message)
        {
            if (_isClosed)
                throw new ChannelClosedException();

            lock (_sendLocker)
            {
                _isReadyToSend = true;

                PumpListeners();

                _readyToReceive.WaitOne();
                _isReadyToReceive = false;
                if (_isClosed)
                    throw new ChannelClosedException();
                _data = message;
                _sent.Set();
                _receiveComplete.WaitOne();
            }
        }

        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready.
        /// </summary>
        /// <returns>The message received.</returns>
        public T Receive()
        {
            if (_isClosed)
                throw new ChannelClosedException();

            T data;
            lock (_receiveLocker)
            {
                _isReadyToReceive = true;
                _readyToReceive.Set();

                PumpListeners();

                _sent.WaitOne();
                _isReadyToSend = false;
                if (_isClosed)
                    throw new ChannelClosedException();
                data = _data;
                _receiveComplete.Set();
            }

            return data;
        }

        public IEnumerator<T> GetEnumerator()
        {
            while (true)
            {
                T value;

                try
                {
                    value = Receive();
                }
                catch (ChannelClosedException)
                {
                    yield break;
                }

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

                PumpListeners();

                _sent.Set();
                _readyToReceive.Set();

                lock (_receiveLocker)
                {
                    lock (_sendLocker)
                    {
                        _readyToReceive.Dispose();
                        _sent.Dispose();
                        _receiveComplete.Dispose();
                    }
                }
            }
        }

        private void PumpListeners()
        {
            lock (_listeners)
            {
                foreach (var listener in _listeners)
                {
                    listener.Set();
                }
            }
        }

        bool IChan.TryLockReceive()
        {
            return Monitor.TryEnter(_receiveLocker);
        }

        bool IChan.TryLockSend()
        {
            return Monitor.TryEnter(_sendLocker);
        }

        void IChan.UnlockReceive()
        {
            Monitor.Exit(_receiveLocker);
        }

        void IChan.UnlockSend()
        {
            Monitor.Exit(_sendLocker);
        }

        bool IChan.IsReadyToReceive
        {
            get { return _isReadyToReceive; }
        }

        bool IChan.IsReadyToSend
        {
            get { return _isReadyToSend; }
        }

        bool IChan.IsClosed
        {
            get { return _isClosed; }
        }

        void IChan.Send(object message)
        {
            Send((T)message);
        }

        object IChan.Receive()
        {
            return Receive();
        }

        void IChan.AddListener(AutoResetEvent func)
        {
            lock (_listeners)
            {
                _listeners.Add(func);
            }
        }

        void IChan.RemoveListener(AutoResetEvent autoResetEvent)
        {
            lock (_listeners)
            {
                _listeners.Remove(autoResetEvent);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
