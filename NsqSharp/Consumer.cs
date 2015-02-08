using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using NsqSharp.Channels;
using NsqSharp.Go;

namespace NsqSharp
{
    /// <summary>
    /// IHandler is the message processing interface for <see cref="Consumer" />
    ///
    /// Implement this interface for handlers that return whether or not message
    /// processing completed successfully.
    /// 
    /// When the return value is nil Consumer will automatically handle FINishing.
    ///
    /// When the returned value is non-nil Consumer will automatically handle REQueing.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Handles messages.
        /// </summary>
        /// <param name="message">The message.</param>
        void HandleMessage(Message message);
    }

    /// <summary>
    /// HandlerFunc is a convenience type to avoid having to declare a struct
    /// to implement the Handler interface, it can be used like this:
    ///
    /// 	consumer.AddHandler(new HandlerFunc(m => {
    /// 		// handle the message
    /// 	}));
    /// </summary>
    /// <param name="message">The message.</param>
    public delegate void HandlerFunc(Message message);

    /// <summary>
    /// DiscoveryFilter is an interface accepted by `SetBehaviorDelegate()`
    /// for filtering the nsqds returned from discovery via nsqlookupd
    /// </summary>
    public interface IDiscoveryFilter
    {
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="nsqds">TODO</param>
        /// <returns>TODO</returns>
        string[] Filter(string[] nsqds);
    }

    /// <summary>
    /// FailedMessageLogger is an interface that can be implemented by handlers that wish
    /// to receive a callback when a message is deemed "failed" (i.e. the number of attempts
    /// exceeded the Consumer specified MaxAttemptCount)
    /// </summary>
    public interface IFailedMessageLogger
    {
        /// <summary>
        /// Called when a message is deemed "failed" (i.e. the number of attempts
        /// exceeded the Consumer specified MaxAttemptCount)
        /// </summary>
        /// <param name="message">The failed message.</param>
        void LogFailedMessage(Message message);
    }

    /// <summary>
    /// ConsumerStats represents a snapshot of the state of a Consumer's connections and the messages
    /// it has seen
    /// </summary>
    public class ConsumerStats
    {
        /// <summary>Messages Received</summary>
        public ulong MessagesReceived { get; set; }
        /// <summary>Messages Finished</summary>
        public ulong MessagesFinished { get; set; }
        /// <summary>Messages Requeued</summary>
        public ulong MessagesRequeued { get; set; }
        /// <summary>Connections</summary>
        public int Connections { get; set; }
    }

    /// <summary>
    /// Consumer is a high-level type to consume from NSQ.
    ///
    /// A Consumer instance is supplied a Handler that will be executed
    /// concurrently via goroutines to handle processing the stream of messages
    /// consumed from the specified topic/channel. See: Handler/HandlerFunc
    /// for details on implementing the interface to create handlers.
    ///
    /// If configured, it will poll nsqlookupd instances and handle connection (and
    /// reconnection) to any discovered nsqds.
    /// </summary>
    public class Consumer
    {
        private static long _instCount;

        private ulong _messagesReceived;
        private ulong _messagesFinished;
        private ulong _messagesRequeued;
        private long _totalRdyCount;
        private long _backoffDuration;
        private int _maxInFlight;

        private readonly ReaderWriterLockSlim _mtx = new ReaderWriterLockSlim();

        private ILogger _logger;
        private LogLevel _logLvl;

        private IDiscoveryFilter _behaviorDelegate;

        private readonly long _id;
        private readonly string _topic;
        private readonly string _channel;
        private readonly Config _config;

        private readonly RNGCryptoServiceProvider _rng; // TODO: must Dispose

        private int _needRDYRedistributed;

        private readonly ReaderWriterLockSlim _backoffMtx = new ReaderWriterLockSlim();
        private int _backoffCounter;

        private readonly Chan<Message> _incomingMessages;

        private readonly ReaderWriterLockSlim _rdyRetryMtx = new ReaderWriterLockSlim();
        private readonly Dictionary<string, Timer> _rdyRetryTimers;

        private readonly Dictionary<string, Conn> _pendingConnections;
        private readonly Dictionary<string, Conn> _connections;

        private readonly List<string> _nsqdTCPAddrs;

        // used at connection close to force a possible reconnect
        private readonly Chan<int> _lookupdRecheckChan;
        private readonly List<string> _lookupdHTTPAddrs = new List<string>();
        private int _lookupdQueryIndex;

        private readonly WaitGroup _wg = new WaitGroup();
        private int _runningHandlers;
        private int _stopFlag;
        private int _connectedFlag;
        private readonly Once _stopHandler = new Once();
        private readonly Once _exitHandler = new Once();

        // read from this channel to block until consumer is cleanly stopped
        private readonly Chan<int> _StopChan;
        private readonly Chan<int> _exitChan;

        /// <summary>
        /// Creates a new instance of Consumer for the specified topic/channel
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <param name="config">The config. After config is passed in the values
        /// are no longer mutable (they are copied).</param>
        public Consumer(string topic, string channel, Config config)
        {
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException("channel");
            if (config == null)
                throw new ArgumentNullException("config");

            config.Validate();

            if (!Protocol.IsValidTopicName(topic))
            {
                throw new ArgumentException("invalid topic name", "topic");
            }

            if (!Protocol.IsValidChannelName(channel))
            {
                throw new ArgumentException("invalid channel name", "channel");
            }

            _id = Interlocked.Increment(ref _instCount);

            _topic = topic;
            _channel = channel;
            _config = config.Clone();

            _logger = new Logger(); // TODO: writes to stderr, not console
            _logLvl = LogLevel.Info;
            _maxInFlight = config.MaxInFlight;

            _incomingMessages = new Chan<Message>();

            _rdyRetryTimers = new Dictionary<string, Timer>();
            _pendingConnections = new Dictionary<string, Conn>();
            _connections = new Dictionary<string, Conn>();

            _lookupdRecheckChan = new Chan<int>(bufferSize: 1);

            _rng = new RNGCryptoServiceProvider();

            _StopChan = new Chan<int>();
            _exitChan = new Chan<int>();

            _wg.Add(1);

            GoFunc.Run(rdyLoop);
        }

        /// <summary>Stats retrieves the current connection and message statistics for a Consumer</summary>
        public ConsumerStats Stats()
        {
            return new ConsumerStats
                   {
                       MessagesReceived = _messagesReceived,
                       MessagesFinished = _messagesFinished,
                       MessagesRequeued = _messagesRequeued,
                       Connections = conns().Count
                   };
        }

        private List<Conn> conns()
        {
            _mtx.EnterReadLock();
            try
            {
                return new List<Conn>(_connections.Values);
            }
            finally
            {
                _mtx.ExitReadLock();
            }
        }

        /// <summary>
        /// SetLogger assigns the logger to use as well as a level
        ///
        /// The logger parameter is an interface that requires the following
        /// method to be implemented (such as the the stdlib log.Logger):
        ///
        ///    Output(calldepth int, s string)
        ///
        /// </summary>
        /// <param name="l">The <see cref="ILogger"/></param>
        /// <param name="lvl">The <see cref="LogLevel"/></param>
        public void SetLogger(ILogger l, LogLevel lvl)
        {
            _logger = l;
            _logLvl = lvl;
        }

        /// <summary>
        /// SetBehaviorDelegate takes a type implementing one or more
        /// of the following interfaces that modify the behavior
        /// of the `Consumer`:
        ///
        ///    DiscoveryFilter
        ///
        /// </summary>
        /// <param name="cb">The callback</param>
        public void SetBehaviorDelegate(IDiscoveryFilter cb)
        {
            // TODO: can go-nsq take a DiscoveryFilter instead of interface{} ?
            _behaviorDelegate = cb;
        }

        /// <summary>
        /// perConnMaxInFlight calculates the per-connection max-in-flight count.
        ///
        /// This may change dynamically based on the number of connections to nsqd the Consumer
        /// is responsible for.
        /// </summary>
        private long perConnMaxInFlighht()
        {
            long b = getMaxInFlight();
            int connCount = conns().Count;
            long s = (connCount == 0 ? 0 : b/connCount);
            return Math.Min(Math.Max(1, s), b);
        }

        /// <summary>
        /// IsStarved indicates whether any connections for this consumer are blocked on processing
        /// before being able to receive more messages (ie. RDY count of 0 and not exiting)
        /// </summary>
        public bool IsStarved()
        {
            foreach (var conn in conns())
            {
                long threshold = (long)(conn._lastRdyCount*0.85);
                long inFlight = conn._messagesInFlight;
                if (inFlight >= threshold && inFlight > 0 && !conn.IsClosing)
                {
                    return true;
                }
            }
            return false;
        }

        private int getMaxInFlight()
        {
            return _maxInFlight;
        }

        /// <summary>
        /// ChangeMaxInFlight sets a new maximum number of messages this comsumer instance
        /// will allow in-flight, and updates all existing connections as appropriate.
        ///
        /// For example, ChangeMaxInFlight(0) would pause message flow
        ///
        /// If already connected, it updates the reader RDY state for each connection.
        /// </summary>
        public void ChangeMaxInFlight(int maxInFlight)
        {
            if (getMaxInFlight() == _maxInFlight)
                return;

            _maxInFlight = maxInFlight;

            foreach (var c in conns())
            {
                maybeUpdateRDY(c);
            }
        }

        // TODO

        private void rdyLoop()
        {
            var redistributeTicker = new Ticker(TimeSpan.FromSeconds(5));

            bool doLoop = true;
            while (doLoop)
            {
                Select
                    .CaseReceive(redistributeTicker.C, o => redistributeRDY())
                    .CaseReceive(_exitChan, o => doLoop = false)
                    .NoDefault();
            }

            redistributeTicker.Stop();
            log(LogLevel.Info, "rdyLoop exiting");
            _wg.Done();
        }

        // TODO

        private void exit()
        {
            _exitHandler.Do(() =>
            {
                _exitChan.Close();
                _wg.Wait();
                _StopChan.Close();
            });
        }

        private void log(LogLevel lvl, string line, params object[] args)
        {
            // TODO: fix race condition on logger
            var logger = _logger;
            if (logger == null)
                return;

            if (_logLvl > lvl)
                return;

            // TODO: proper width formatting
            logger.Output(2, string.Format("{0} {1} [{2}/{3}] {4}", 
                Log.Prefix(lvl), _id, _topic, _channel,
                string.Format(line, args)));
        }
    }
}
