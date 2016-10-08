using System;
using System.Threading;
using NsqSharp.Utils.Channels;

namespace NsqSharp.Utils
{
    /// <summary>
    /// The <see cref="Timer"/> type represents a single event. When the <see cref="Timer"/> expires, the current time
    /// will be sent on <see cref="C"/>, unless the <see cref="Timer"/> was created by <see cref="Time.AfterFunc"/>.
    /// </summary>
    public class Timer
    {
        private readonly Chan<DateTime> _timerChan = new Chan<DateTime>();
        private int _isTimerActive;

        /// <summary>
        /// Creates a new <see cref="Timer"/> that will send the current time on its channel <see cref="C"/> after at least
        /// <paramref name="duration" />.
        /// </summary>
        public Timer(TimeSpan duration)
        {
            _isTimerActive = 1;

            GoFunc.Run(() =>
            {
                bool ok;
                Time.After(duration).ReceiveOk(out ok);
                if (ok)
                {
                    if (Interlocked.CompareExchange(ref _isTimerActive, 0, 1) == 1)
                    {
                        _timerChan.Send(DateTime.Now);
                        _timerChan.Close();
                    }
                }
            }, string.Format("timer started:{0} duration:{1}", DateTime.Now, duration));
        }

        /// <summary>
        /// The channel the Timer will fire on after the duration.
        /// </summary>
        public IReceiveOnlyChan<DateTime> C
        {
            get { return _timerChan; }
        }

        /// <summary>
        /// Stop prevents the Timer from firing. It returns <c>true</c> if the call stops the timer, <c>false</c> if the timer
        /// has already expired or been stopped. Stop does not close the channel, to prevent a read from the channel succeeding
        /// incorrectly.
        /// </summary>
        public bool Stop()
        {
            if (Interlocked.CompareExchange(ref _isTimerActive, 0, 1) == 0)
                return false;

            _timerChan.Close();

            return true;
        }
    }
}
