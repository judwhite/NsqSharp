using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NsqSharp.Channels;
using NsqSharp.Go;
using NsqSharp.Utils;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/master/producer.go

    /// <summary>
    /// IConn interface
    /// </summary>
    public interface IConn
    {
        /// <summary>
        /// SetLogger assigns the logger to use as well as a level.
        ///
        /// The format parameter is expected to be a printf compatible string with
        /// a single {0} argument.  This is useful if you want to provide additional
        /// context to the log messages that the connection will print, the default
        /// is '({0})'.
        /// </summary>
        void SetLogger(ILogger l, LogLevel lvl, string format);

        /// <summary>
        /// Connect dials and bootstraps the nsqd connection
        /// (including IDENTIFY) and returns the IdentifyResponse
        /// </summary>
        IdentifyResponse Connect();

        /// <summary>
        /// Close idempotently initiates connection close
        /// </summary>
        void Close();

        /// <summary>
        /// WriteCommand is a thread safe method to write a Command
        /// to this connection, and flush.
        /// </summary>
        void WriteCommand(Command command);
    }

    /// <summary>
    /// Producer is a high-level type to publish to NSQ.
    ///
    /// A Producer instance is 1:1 with a destination `nsqd`
    /// and will lazily connect to that instance (and re-connect)
    /// when Publish commands are executed.
    /// </summary>
    public partial class Producer
    {
        internal long _id;
        private readonly string _addr;
        private IConn _conn;
        private readonly Config _config;

        private ILogger _logger;
        private LogLevel _logLvl;

        private readonly Chan<byte[]> _responseChan;
        private readonly Chan<byte[]> _errorChan;
        private Chan<int> _closeChan;

        private readonly Chan<ProducerTransaction> _transactionChan;
        private readonly Queue<ProducerTransaction> _transactions = new Queue<ProducerTransaction>();
        private int _state;

        private int _concurrentProducers;
        private int _stopFlag;
        private readonly Chan<int> _exitChan;
        private readonly WaitGroup _wg = new WaitGroup();
        private readonly object _guard = new object();

        private readonly Func<Producer, IConn> _connFactory;
    }

    /// <summary>
    /// ProducerTransaction is returned by the async publish methods
    /// to retrieve metadata about the command after the
    /// response is received.
    /// </summary>
    public class ProducerTransaction
    {
        internal Command _cmd;
        internal Chan<ProducerTransaction> _doneChan;

        /// <summary>
        /// the error (or nil) of the publish command
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// the slice of variadic arguments passed to PublishAsync or MultiPublishAsync
        /// </summary>
        public object[] Args { get; set; }

        internal void finish()
        {
            if (_doneChan != null)
                _doneChan.Send(this);
        }
    }

    public partial class Producer : IConnDelegate
    {
        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="addr">The address.</param>
        public Producer(string addr)
            : this(addr, new Config())
        {
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="addr">The address.</param>
        /// <param name="config">The config. After Config is passed into NewProducer the values are
        /// no longer mutable (they are copied).</param>
        public Producer(string addr, Config config)
            : this(addr, config, null)
        {
        }

        private Producer(string addr, Config config, Func<Producer, IConn> connFactory)
        {
            if (string.IsNullOrEmpty(addr))
                throw new ArgumentNullException("addr");
            if (config == null)
                throw new ArgumentNullException("config");

            _id = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds % 10000; // TODO: Remove

            config.Validate();

            _addr = addr;
            _config = config.Clone();

            _logger = new ConsoleLogger(); // TODO
            _logLvl = LogLevel.Info;

            _transactionChan = new Chan<ProducerTransaction>();
            _exitChan = new Chan<int>();
            _responseChan = new Chan<byte[]>();
            _errorChan = new Chan<byte[]>();

            if (connFactory == null)
                connFactory = p => new Conn(_addr, _config, p);

            _connFactory = connFactory;
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="addr">The address.</param>
        /// <param name="config">The config. After Config is passed into NewProducer the values are
        /// no longer mutable (they are copied).</param>
        /// <param name="connFactory">The method to create a new connection (used for mocking)</param>
        public static Producer Create(string addr, Config config, Func<Producer, IConn> connFactory)
        {
            return new Producer(addr, config, connFactory);
        }

        /// <summary>
        /// Ping causes the Producer to connect to it's configured nsqd (if not already
        /// connected) and send a `Nop` command, returning any error that might occur.
        ///
        /// This method can be used to verify that a newly-created Producer instance is
        /// configured correctly, rather than relying on the lazy "connect on Publish"
        /// behavior of a Producer.
        /// </summary>
        public void Ping()
        {
            Connect();

            // TODO: PR: go-nsq, what does writing NO_OP prove above just Connect? nsqd does not respond to NO_OP
            _conn.WriteCommand(Command.Nop());
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
        /// String returns the address of the Producer
        /// </summary>
        /// <returns>The address of the Producer</returns>
        public override string ToString()
        {
            return _addr;
        }

        /// <summary>
        /// Stop initiates a graceful stop of the Producer (permanent)
        ///
        /// NOTE: this blocks until completion
        /// </summary>
        public void Stop()
        {
            lock (_guard)
            {
                if (Interlocked.CompareExchange(ref _stopFlag, value: 1, comparand: 0) != 0)
                {
                    // already closed
                    return;
                }
                log(LogLevel.Info, "stopping");
                _exitChan.Close();
                close();
                _wg.Wait();
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// PublishAsync publishes a message body to the specified topic
        /// but does not wait for the response from `nsqd`.
        ///
        /// When the Producer eventually receives the response from `nsqd`,
        /// the supplied `doneChan` (if specified)
        /// will receive a `ProducerTransaction` instance with the supplied variadic arguments
        /// and the response error if present
        /// </summary>
        private void PublishAsync(string topic, byte[] body, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            sendCommandAsync(Command.Publish(topic, body), doneChan, args);
        }

        /// <summary>
        /// PublishAsync publishes a message body to the specified topic
        /// but does not wait for the response from `nsqd`.
        ///
        /// When the Producer eventually receives the response from `nsqd`,
        /// the supplied `doneChan` (if specified)
        /// will receive a `ProducerTransaction` instance with the supplied variadic arguments
        /// and the response error if present
        /// </summary>
        private void PublishAsync(string topic, string value, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            sendCommandAsync(Command.Publish(topic, Encoding.UTF8.GetBytes(value)), doneChan, args);
        }

        /// <summary>
        /// MultiPublishAsync publishes a slice of message bodies to the specified topic
        /// but does not wait for the response from `nsqd`.
        ///
        /// When the Producer eventually receives the response from `nsqd`,
        /// the supplied `doneChan` (if specified)
        /// will receive a `ProducerTransaction` instance with the supplied variadic arguments
        /// and the response error if present
        /// </summary>
        private void MultiPublishAsync(string topic, ICollection<byte[]> body, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            var cmd = Command.MultiPublish(topic, body);
            sendCommandAsync(cmd, doneChan, args);
        }

        /// <summary>
        /// Publish synchronously publishes a message body to the specified topic, returning
        /// an error if publish failed
        /// </summary>
        public void Publish(string topic, byte[] body)
        {
            sendCommand(Command.Publish(topic, body));
        }

        /// <summary>
        /// Publish synchronously publishes a string to the specified topic, returning
        /// an error if publish failed
        /// </summary>
        public void Publish(string topic, string value)
        {
            Publish(topic, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// MultiPublish synchronously publishes a slice of message bodies to the specified topic, returning
        /// the an error if publish failed
        /// </summary>
        public void MultiPublish(string topic, ICollection<byte[]> body)
        {
            var cmd = Command.MultiPublish(topic, body);
            sendCommand(cmd);
        }

        // TODO: temporary until multithreaded issue is figured out
        private readonly object _sendCommandLocker = new object();
        private void sendCommand(Command cmd)
        {
            lock (_sendCommandLocker)
            {
                var doneChan = new Chan<ProducerTransaction>();

                try
                {
                    sendCommandAsync(cmd, doneChan, null);
                }
                catch (Exception)
                {
                    doneChan.Close();
                    throw;
                }

                var t = doneChan.Receive();
                if (t.Error != null)
                    throw t.Error;
            }
        }

        private void sendCommandAsync(Command cmd, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            Interlocked.Increment(ref _concurrentProducers);
            try
            {
                if (_state != (int)State.Connected)
                {
                    Connect();
                }

                var t = new ProducerTransaction
                        {
                            _cmd = cmd,
                            _doneChan = doneChan,
                            Args = args,
                        };

                Select
                    .CaseSend(_transactionChan, t, () => { })
                    .CaseReceive(_exitChan, m => { throw new ErrStopped(); })
                    .NoDefault();
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentProducers);
            }
        }

        /// <summary>
        /// Connect to NSQD
        /// </summary>
        public void Connect()
        {
            lock (_guard)
            {
                if (_stopFlag == 1)
                    throw new ErrStopped();

                if (_state == (int)State.Connected)
                    return;

                const int newValue = (int)State.Connected;
                const int comparand = (int)State.Init;
                if (Interlocked.CompareExchange(ref _state, newValue, comparand) != comparand)
                {
                    throw new ErrNotConnected();
                }

                log(LogLevel.Info, "{0} connecting to nsqd", _addr);

                _conn = _connFactory(this);
                _conn.SetLogger(_logger, _logLvl, string.Format("{0} ({{0}})", _id));
                try
                {
                    _conn.Connect();
                }
                catch (Exception ex)
                {
                    _conn.Close();
                    log(LogLevel.Error, "({0}) error connecting to nsqd - {1}", _addr, ex.Message);
                    _state = (int)State.Init;
                    throw;
                }

                _closeChan = new Chan<int>();
                _wg.Add(1);
                GoFunc.Run(router);
            }
        }

        private void close()
        {
            const int newValue = (int)State.Disconnected;
            const int comparand = (int)State.Connected;
            if (Interlocked.CompareExchange(ref _state, newValue, comparand) != comparand)
            {
                return;
            }

            _conn.Close();

            GoFunc.Run(() =>
            {
                // we need to handle this in a goroutine so we don't
                // block the caller from making progress
                _wg.Wait();
                _state = (int)State.Init;
            });
        }

        private void router()
        {
            bool doLoop = true;

            using (var select =
                Select
                    .CaseReceive(_transactionChan, t =>
                    {
                        _transactions.Enqueue(t);
                        try
                        {
                            _conn.WriteCommand(t._cmd);
                        }
                        catch (Exception ex)
                        {
                            log(LogLevel.Error, "({0}) sending command - {1}", _conn, ex.Message);
                            close();
                        }
                    })
                    .CaseReceive(_responseChan, data =>
                        popTransaction(FrameType.Response, data)
                    )
                    .CaseReceive(_errorChan, data =>
                        popTransaction(FrameType.Error, data)
                    )
                    .CaseReceive(_closeChan, o =>
                    {
                        doLoop = false;
                    })
                    .CaseReceive(_exitChan, o =>
                    {
                        doLoop = false;
                    })
                    .NoDefault(defer: true))
            {
                while (doLoop)
                {
                    select.Execute();
                }
            }

            transactionCleanup();
            _wg.Done();
            log(LogLevel.Info, "exiting router");
        }

        private void popTransaction(FrameType frameType, byte[] data)
        {
            var t = _transactions.Dequeue();
            if (frameType == FrameType.Error)
            {
                t.Error = new ErrProtocol(Encoding.UTF8.GetString(data));
            }
            t.finish();
        }

        private void transactionCleanup()
        {
            // clean up transactions we can easily account for
            foreach (var t in _transactions)
            {
                t.Error = new ErrNotConnected();
                t.finish();
            }
            _transactions.Clear();

            // spin and free up any writes that might have raced
            // with the cleanup process (blocked on writing
            // to transactionChan)
            bool doLoop = true;
            while (doLoop)
            {
                Select
                    .CaseReceive(_transactionChan, t =>
                    {
                        t.Error = new ErrNotConnected();
                        t.finish();
                    })
                    .Default(() =>
                    {
                        // keep spinning until there are 0 concurrent producers
                        if (_concurrentProducers == 0)
                        {
                            doLoop = false;
                            return;
                        }
                        // give the runtime a chance to schedule other racing goroutines
                        Thread.Sleep(TimeSpan.FromMilliseconds(5));
                        // TODO: create PR in go-nsq: is continue necessary in default case?
                    });
            }
        }

        private void log(LogLevel lvl, string line, params object[] args)
        {
            // TODO: fix race condition on w.logger
            var logger = _logger;
            if (logger == null)
                return;

            if (_logLvl > lvl)
                return;

            // TODO: proper width formatting
            logger.Output(string.Format("{0} {1} {2}", Log.Prefix(lvl), _id, string.Format(line, args)));
        }

        void IConnDelegate.OnResponse(Conn c, byte[] data)
        {
            _responseChan.Send(data);
        }

        void IConnDelegate.OnError(Conn c, byte[] data)
        {
            _errorChan.Send(data);
        }

        void IConnDelegate.OnMessage(Conn c, Message m)
        {
            // no-op
        }

        void IConnDelegate.OnMessageFinished(Conn c, Message m)
        {
            // no-op
        }

        void IConnDelegate.OnMessageRequeued(Conn c, Message m)
        {
            // no-op
        }

        void IConnDelegate.OnBackoff(Conn c)
        {
            // no-op
        }

        void IConnDelegate.OnResume(Conn c)
        {
            // no-op
        }

        void IConnDelegate.OnIOError(Conn c, Exception err)
        {
            close();
        }

        void IConnDelegate.OnHeartbeat(Conn c)
        {
            // no-op
        }

        void IConnDelegate.OnClose(Conn c)
        {
            lock (_guard)
            {
                _closeChan.Close();
            }
        }
    }
}
