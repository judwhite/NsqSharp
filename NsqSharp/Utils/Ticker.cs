using System;
using System.Threading;
using NsqSharp.Utils.Channels;

namespace NsqSharp.Utils
{
    /// <summary>
    /// A Ticker holds a channel that delivers `ticks' of a clock at intervals. http://golang.org/pkg/time/#Ticker
    /// </summary>
    public class Ticker
    {
        private readonly Chan<DateTime> _tickerChan = new Chan<DateTime>();
        private readonly object _locker = new object();
        private System.Threading.Timer _threadingTimer;
        private bool _stop;

        /// <summary>
        /// Initializes a new instance of the Ticker class.
        /// </summary>
        /// <param name="duration">The interval between ticks on the channel.</param>
        public Ticker(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("duration", "duration must be > 0");

            var started = DateTime.Now;

            lock (_locker)
            {
                System.Threading.Timer t = new System.Threading.Timer(
                    delegate
                    {
                        Thread.CurrentThread.Name = string.Format("ticker started:{0} duration:{1} tick:{2}",
                            started, duration, DateTime.Now);

                        lock (_locker)
                        {
                            if (_stop)
                            {
                                if (_threadingTimer != null)
                                {
                                    _threadingTimer.Dispose();
                                    _threadingTimer = null;
                                }
                                return;
                            }

                            new SelectCase()
                                .CaseSend(_tickerChan, DateTime.UtcNow)
                                .Default(null);
                        }

                    }, null, duration, duration);

                _threadingTimer = t;
            }
        }

        /// <summary>
        /// Stop turns off a ticker. After Stop, no more ticks will be sent. Stop does not close the channel,
        /// to prevent a read from the channel succeeding incorrectly. See <see cref="Close"/>.
        /// </summary>
        public void Stop()
        {
            lock (_locker)
            {
                _stop = true;
                if (_threadingTimer != null)
                {
                    _threadingTimer.Dispose();
                    _threadingTimer = null;
                }
            }
        }

        /// <summary>
        /// Stops the ticker and closes the channel, exiting the ticker thread. Note: after closing a channel,
        /// all reads from the channel will return default(T). See <see cref="Stop"/>.
        /// </summary>
        public void Close()
        {
            lock (_locker)
            {
                Stop();
                _tickerChan.Close();
            }
        }

        /// <summary>
        /// The channel on which the ticks are delivered.
        /// </summary>
        public IReceiveOnlyChan<DateTime> C
        {
            get { return _tickerChan; }
        }
    }
}
