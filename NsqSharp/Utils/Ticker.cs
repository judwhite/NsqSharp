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
        private readonly Chan<bool> _stopTicker = new Chan<bool>();
        private long _stop;

        /// <summary>
        /// Initializes a new instance of the Ticker class.
        /// </summary>
        /// <param name="duration">The interval between ticks on the channel.</param>
        public Ticker(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("duration", "duration must be > 0");

            GoFunc.Run(() =>
            {                
                while (Interlocked.Read(ref _stop) == 0)
                {
                    new SelectCase()
                        .CaseReceive(Time.After(duration),
                                     _ =>
                                     {
                                         new SelectCase()
                                             .CaseSend(_tickerChan, DateTime.Now)
                                             .Default(null);
                                     })
                        .CaseReceive(_stopTicker)
                        .NoDefault();
                }
            }, string.Format("Ticker started:{0} Tick interval:{1}", DateTime.Now, duration));
        }

        /// <summary>
        /// Stop turns off a ticker. After Stop, no more ticks will be sent. Stop does not close the channel,
        /// to prevent a read from the channel succeeding incorrectly. See <see cref="Close"/>.
        /// </summary>
        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _stop, 1, 0) == 0)
            {
                _stopTicker.Close();
            }
        }

        /// <summary>
        /// Stops the ticker and closes the channel, exiting the ticker thread. Note: after closing a channel,
        /// all reads from the channel will return default(T). See <see cref="Stop"/>.
        /// </summary>
        public void Close()
        {
            Stop();
            _tickerChan.Close();
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
