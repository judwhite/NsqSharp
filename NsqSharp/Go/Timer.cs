using System;
using NsqSharp.Channels;

namespace NsqSharp.Go
{
    /// <summary>
    /// The <see cref="Timer"/> type represents a single event. When the <see cref="Timer"/> expires, the current time
    /// will be sent on <see cref="C"/>, unless the <see cref="Timer"/> was created by <see cref="Time.AfterFunc"/>.
    /// </summary>
    public class Timer
    {
        private readonly Chan<DateTime> _timerChan = new Chan<DateTime>();
        private readonly Chan<bool> _stopChan = new Chan<bool>(bufferSize: 1);
        private bool _isTimerActive;

        /// <summary>
        /// Creates a new <see cref="Timer"/> that will send the current time on its channel <see cref="C"/> after at least
        /// <paramref name="duration" />.
        /// </summary>
        public Timer(TimeSpan duration)
        {
            _isTimerActive = true;

            GoFunc.Run(() =>
                Select
                    .CaseReceive(_stopChan)
                    .CaseReceive(Time.After(duration), o =>
                    {
                        _isTimerActive = false;
                        _timerChan.Send(DateTime.Now);
                        _timerChan.Close();
                    })
                    .NoDefault()
            );
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
            if (_isTimerActive)
                return false;

            _isTimerActive = false;
            _stopChan.Send(true);

            return true;
        }
    }
}
