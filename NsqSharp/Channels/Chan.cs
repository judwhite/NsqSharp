using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace NsqSharp.Channels
{
    /// <summary>
    /// Channel for synchronizing communication between threads. Supports foreach to read from the channel until it's closed. See also <see cref="Select"/>.
    /// </summary>
    /// <typeparam name="T">The message type communicated over the channel.</typeparam>
    public class Chan<T> : IReceiveOnlyChan<T>, ISendOnlyChan<T>, IEnumerable<T>
    {
        private readonly object _sendLocker = new object();
        private readonly object _receiveLocker = new object();
        private readonly object _isClosedLocker = new object();

        private readonly AutoResetEvent _readyToReceive = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent _sent = new AutoResetEvent(initialState: false);
        private readonly AutoResetEvent _receiveComplete = new AutoResetEvent(initialState: false);

        private readonly List<AutoResetEvent> _listeners = new List<AutoResetEvent>();

        private readonly TimeSpan _infiniteTimeSpan = TimeSpan.FromMilliseconds(-1);

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
            ((ISendOnlyChan)this).TrySend(message, _infiniteTimeSpan);
        }

        bool ISendOnlyChan.TrySend(object message, TimeSpan timeout)
        {
            if (_isClosed)
                throw new ChannelClosedException();

            lock (_sendLocker)
            {
                _isReadyToSend = true;

                PumpListeners();

                bool success = _readyToReceive.WaitOne(timeout.Milliseconds);
                _isReadyToReceive = false;
                if (!success)
                    return false;
                if (_isClosed)
                    throw new ChannelClosedException();
                _data = (T)message;
                _sent.Set();
                _receiveComplete.WaitOne();
            }

            return true;
        }

        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready or the channel is closed.
        /// </summary>
        /// <returns>The message received; or default(T) if the channel was closed.</returns>
        public T Receive()
        {
            var valueOk = ReceiveOk();
            return valueOk.Value;
        }

        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready or the channel is closed.
        /// </summary>
        /// <returns>The message received; or default(T) if the channel was closed. Includes an indicator
        /// whether the channel was closed or not.</returns>
        public ReceiveOk<T> ReceiveOk()
        {
            if (_isClosed)
                return new ReceiveOk<T> { Value = default(T), Ok = false };

            T data;
            lock (_receiveLocker)
            {
                _isReadyToReceive = true;
                _readyToReceive.Set();

                PumpListeners();

                _sent.WaitOne();
                _isReadyToSend = false;
                data = _data;
                _receiveComplete.Set();
            }

            // TODO: Race condition, but can't lock _isClosedLocker in this method. Fix.
            if (_isClosed)
                return new ReceiveOk<T> { Value = default(T), Ok = false };
            else
                return new ReceiveOk<T> { Value = data, Ok = true };
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            while (true)
            {
                var valueOk = ReceiveOk();

                if (!valueOk.Ok)
                    yield break;
                else
                    yield return valueOk.Value;
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

        bool ISendOnlyChan.IsReadyToReceive
        {
            get { return _isReadyToReceive; }
        }

        bool IReceiveOnlyChan.IsReadyToSend
        {
            get { return _isReadyToSend || _isClosed; }
        }

        bool IChan.IsClosed
        {
            get { return _isClosed; }
        }

        void ISendOnlyChan.Send(object message)
        {
            Send((T)message);
        }

        object IReceiveOnlyChan.Receive()
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

    /// <summary>
    /// <see cref="Value"/>, <see cref="Ok"/> return type from <see cref="Chan&lt;T&gt;.ReceiveOk"/>
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    public class ReceiveOk<T>
    {
        /// <summary>The value</summary>
        public T Value { get; set; }
        /// <summary><c>true</c> if the value was read from a sent value; <c>false</c> if the channel is closed</summary>
        public bool Ok { get; set; }
    }
}
