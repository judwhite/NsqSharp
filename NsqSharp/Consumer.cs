using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Web;
using Newtonsoft.Json.Linq;
using NsqSharp.Channels;
using NsqSharp.Extensions;
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
        /// Filters a list of NSQD addresses.
        /// </summary>
        Collection<string> Filter(IEnumerable<string> nsqds);
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

        private readonly RNGCryptoServiceProvider _rng; // TODO: Dispose, or make static

        private int _needRDYRedistributed;

        private readonly ReaderWriterLockSlim _backoffMtx = new ReaderWriterLockSlim(); // TODO: Dispose
        private int _backoffCounter;

        private readonly Chan<Message> _incomingMessages;

        private readonly ReaderWriterLockSlim _rdyRetryMtx = new ReaderWriterLockSlim(); // TODO: Dispose
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
            long s = (connCount == 0 ? 0 : b / connCount);
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
                long threshold = (long)(conn._lastRdyCount * 0.85);
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

        /// <summary>
        /// ConnectToNSQLookupd adds an nsqlookupd address to the list for this Consumer instance.
        ///
        /// If it is the first to be added, it initiates an HTTP request to discover nsqd
        /// producers for the configured topic.
        ///
        /// A goroutine is spawned to handle continual polling.
        /// </summary>
        public void ConnectToNSQLookupd(string addr)
        {
            if (string.IsNullOrEmpty(addr))
                throw new ArgumentNullException("addr");

            if (_stopFlag == 1)
                throw new Exception("consumer stopped");
            if (_runningHandlers == 0)
                throw new Exception("no handlers");

            validatedLookupAddr(addr);

            _connectedFlag = 1;

            int numLookupd;
            _mtx.EnterWriteLock();
            try
            {
                foreach (var x in _lookupdHTTPAddrs)
                {
                    if (x == addr)
                        return;
                }

                _lookupdHTTPAddrs.Add(addr);
                numLookupd = _lookupdHTTPAddrs.Count;
            }
            finally
            {
                _mtx.ExitWriteLock();
            }

            // if this is the first one, kick off the go loop
            if (numLookupd == 1)
            {
                queryLookupd();
                _wg.Add(1);
                GoFunc.Run(lookupdLoop);
            }
        }

        /// <summary>
        /// ConnectToNSQLookupd adds multiple nsqlookupd address to the list for this Consumer instance.
        ///
        /// If adding the first address it initiates an HTTP request to discover nsqd
        /// producers for the configured topic.
        ///
        /// A goroutine is spawned to handle continual polling.
        /// </summary>
        public void ConnectToNSQLookupds(IEnumerable<string> addresses)
        {
            if (addresses == null)
                throw new ArgumentNullException("addresses");

            foreach (var addr in addresses)
            {
                ConnectToNSQLookupd(addr);
            }
        }

        private void validatedLookupAddr(string addr)
        {
            if (addr.Contains("/"))
            {
                // TODO: verify this is the kind of validation we want
                new Uri(addr, UriKind.Absolute);
            }
            if (!addr.Contains(":"))
                throw new Exception("missing port");
        }

        /// <summary>
        /// poll all known lookup servers every LookupdPollInterval
        /// </summary>
        private void lookupdLoop()
        {
            // add some jitter so that multiple consumers discovering the same topic,
            // when restarted at the same time, dont all connect at once.
            var jitter = new TimeSpan((long)(_rng.Float64() * _config.LookupdPollJitter * _config.LookupdPollInterval.Ticks));

            bool doLoop = true;

            Select
                .CaseReceive(Time.After(jitter))
                .CaseReceive(_exitChan, o => doLoop = false)
                .NoDefault();

            var ticker = new Ticker(_config.LookupdPollInterval);

            using (var select =
                    Select
                        .CaseReceive(ticker.C, o => queryLookupd())
                        .CaseReceive(_lookupdRecheckChan, o => queryLookupd())
                        .CaseReceive(_exitChan, o => doLoop = false)
                        .NoDefault(defer: true))
            {
                while (doLoop)
                {
                    select.Execute();
                }
            }

            ticker.Stop();
            log(LogLevel.Info, "exiting lookupdLoop");
            _wg.Done();
        }

        private string nextLookupdEndpoint()
        {
            string addr;
            int num;

            _mtx.EnterReadLock();
            try
            {
                addr = _lookupdHTTPAddrs[_lookupdQueryIndex];
                num = _lookupdHTTPAddrs.Count;
            }
            finally
            {
                _mtx.ExitReadLock();
            }

            _lookupdQueryIndex = (_lookupdQueryIndex + 1) % num;

            string urlString = addr;
            if (!urlString.Contains("://"))
                urlString = "http://" + addr;

            // TODO: handle parsing url better... maybe
            var u = new Uri(urlString);
            urlString = string.Format("{0}://{1}/lookup?topic={2}", u.Scheme, u.Authority, _topic);
            return urlString;
        }

        private void queryLookupd()
        {
            string endpoint = nextLookupdEndpoint();

            log(LogLevel.Info, "querying nsqlookupd {0}", endpoint);

            JObject data;
            try
            {
                data = ApiRequest.NegotiateV1("GET", endpoint);
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, "error querying nsqlookupd ({0}) - {1}", endpoint, ex);
                return;
            }

            var nsqAddrs = new Collection<string>();
            foreach (var producer in data["producers"])
            {
                var broadcastAddress = (string)producer["broadcast_address"];
                var port = (int)producer["tcp_port"];
                var joined = string.Format("{0}:{1}", broadcastAddress, port);
                nsqAddrs.Add(joined);
            }

            if (_behaviorDelegate != null)
            {
                nsqAddrs = _behaviorDelegate.Filter(nsqAddrs);
            }

            foreach (var addr in nsqAddrs)
            {
                try
                {
                    ConnectToNSQLookupd(addr);
                }
                catch (ErrAlreadyConnected)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    log(LogLevel.Error, "({0}) error connecting to nsqd - {1}", addr, ex);
                }
            }
        }

        // TODO

        private void rdyLoop()
        {
            var redistributeTicker = new Ticker(TimeSpan.FromSeconds(5));

            bool doLoop = true;
            using (var select =
                    Select
                        .CaseReceive(redistributeTicker.C, o => redistributeRDY())
                        .CaseReceive(_exitChan, o => doLoop = false)
                        .NoDefault(defer: true))
            {
                while (doLoop)
                {
                    select.Execute();
                }
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
