using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Loggers;

namespace NsqSharp
{
    // https://github.com/nsqio/go-nsq/blob/master/producer.go

    /// <summary>
    /// IConn interface
    /// </summary>
    internal interface IConn
    {
        /// <summary>
        /// SetLogger assigns the logger to use as well as a level.
        ///
        /// The format parameter is expected to be a printf compatible string with
        /// a single {0} argument.  This is useful if you want to provide additional
        /// context to the log messages that the connection will print, the default
        /// is '({0})'.
        /// </summary>
        void SetLogger(Core.ILogger l, string format);

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
    /// <para>Producer is a high-level type to publish to NSQ.</para>
    ///
    /// <para>A Producer instance is 1:1 with a destination nsqd
    /// and will lazily connect to that instance (and re-connect)
    /// when Publish commands are executed.</para>
    /// <seealso cref="Publish(string, string)"/>
    /// <seealso cref="Publish(string, byte[])"/>
    /// <seealso cref="Stop"/>
    /// </summary>
    public sealed partial class Producer
    {
        private static long _instCount;

        internal long _id;
        private readonly string _addr;
        private IConn _conn;
        private readonly Config _config;

        private readonly Core.ILogger _logger;

        private readonly Chan<byte[]> _responseChan;
        private readonly Chan<byte[]> _errorChan;
        private Chan<int> _closeChan;

        private readonly Chan<ProducerResponse> _transactionChan;
        private readonly Queue<ProducerResponse> _transactions = new Queue<ProducerResponse>();
        private int _state;

        private int _concurrentProducers;
        private int _stopFlag;
        private readonly Chan<int> _exitChan;
        private readonly WaitGroup _wg = new WaitGroup();
        private readonly object _guard = new object();

        private readonly Func<Producer, IConn> _connFactory;
    }

    /// <summary>
    /// ProducerResponse is returned by the async publish methods
    /// to retrieve metadata about the command after the
    /// response is received.
    /// </summary>
    public class ProducerResponse
    {
        internal Command _cmd;
        internal Chan<ProducerResponse> _doneChan;

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

    public sealed partial class Producer : IConnDelegate
    {
        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="nsqdAddress">The nsqd address.</param>
        public Producer(string nsqdAddress)
            : this(nsqdAddress, new Config())
        {
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="nsqdAddress">The nsqd address.</param>
        /// <param name="config">The config. After Config is passed in the values are
        /// no longer mutable (they are copied).</param>
        public Producer(string nsqdAddress, Config config)
            : this(nsqdAddress, new ConsoleLogger(Core.LogLevel.Info), config, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="nsqdAddress">The nsqd address.</param>
        /// <param name="logger">
        /// The logger. Default = <see cref="ConsoleLogger"/>(<see cref="E:Core.LogLevel.Info"/>).
        /// </param>
        public Producer(string nsqdAddress, Core.ILogger logger)
            : this(nsqdAddress, logger, new Config(), null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="nsqdAddress">The nsqd address.</param>
        /// <param name="logger">
        /// The logger. Default = <see cref="ConsoleLogger"/>(<see cref="E:Core.LogLevel.Info"/>).
        /// </param>
        /// <param name="config">The config. Values are copied, changing the properties on <paramref name="config"/>
        /// after the constructor is called will have no effect on the <see cref="Producer"/>.</param>
        public Producer(string nsqdAddress, Core.ILogger logger, Config config)
            : this(nsqdAddress, logger, config, null)
        {
        }

        private Producer(string addr, Core.ILogger logger, Config config, Func<Producer, IConn> connFactory)
        {
            if (string.IsNullOrEmpty(addr))
                throw new ArgumentNullException("addr");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (config == null)
                throw new ArgumentNullException("config");

            _id = Interlocked.Increment(ref _instCount);

            config.Validate();

            _addr = addr;
            _config = config.Clone();

            _logger = logger;

            _transactionChan = new Chan<ProducerResponse>();
            _exitChan = new Chan<int>();
            _responseChan = new Chan<byte[]>();
            _errorChan = new Chan<byte[]>();

            if (connFactory == null)
                connFactory = p => new Conn(_addr, _config, p);

            _connFactory = connFactory;
        }

        /// <summary>Returns the address of the Producer.</summary>
        /// <returns>The address of the Producer.</returns>
        public override string ToString()
        {
            return _addr;
        }

        /// <summary>
        /// <para>Stop initiates a graceful stop of the Producer (permanent).</para>
        ///
        /// <para>NOTE: this blocks until completion</para>
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
                log(Core.LogLevel.Info, "stopping");
                _exitChan.Close();
                close();
                _wg.Wait();
                _logger.Flush();
                Thread.Sleep(500);
            }
        }

        /// <summary>
        ///     <para>Publishes a message <paramref name="body"/> to the specified <paramref name="topic"/>
        ///     but does not wait for the response from nsqd.</para>
        ///     
        ///     <para>When the Producer eventually receives the response from nsqd, the Task will return a
        ///     <see cref="ProducerResponse"/> instance with the supplied <paramref name="args"/> and the response error if
        ///     present.</para>
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="body">The message body.</param>
        /// <param name="args">A variable-length parameters list containing arguments. These arguments will be returned on
        ///     <see cref="ProducerResponse.Args"/>.
        /// </param>
        /// <returns>A Task&lt;ProducerResponse&gt; which can be awaited.</returns>
        public Task<ProducerResponse> PublishAsync(string topic, byte[] body, params object[] args)
        {
            var doneChan = new Chan<ProducerResponse>();
            sendCommandAsync(Command.Publish(topic, body), doneChan, args);
            return Task.Factory.StartNew(() => doneChan.Receive());
        }

        /// <summary>
        ///     <para>Publishes a string <paramref name="value"/> message to the specified <paramref name="topic"/>
        ///     but does not wait for the response from nsqd.</para>
        ///     
        ///     <para>When the Producer eventually receives the response from nsqd, the Task will return a
        ///     <see cref="ProducerResponse"/> instance with the supplied <paramref name="args"/> and the response error if
        ///     present.</para>
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="value">The message body.</param>
        /// <param name="args">A variable-length parameters list containing arguments. These arguments will be returned on
        ///     <see cref="ProducerResponse.Args"/>.
        /// </param>
        /// <returns>A Task&lt;ProducerResponse&gt; which can be awaited.</returns>
        public Task<ProducerResponse> PublishAsync(string topic, string value, params object[] args)
        {
            return PublishAsync(topic, Encoding.UTF8.GetBytes(value), args);
        }

        /// <summary>
        ///     <para>Publishes a collection of message <paramref name="bodies"/> to the specified <paramref name="topic"/>
        ///     but does not wait for the response from nsqd.</para>
        ///     
        ///     <para>When the Producer eventually receives the response from nsqd, the Task will return a
        ///     <see cref="ProducerResponse"/> instance with the supplied <paramref name="args"/> and the response error if
        ///     present.</para>
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="bodies">The collection of message bodies.</param>
        /// <param name="args">A variable-length parameters list containing arguments. These arguments will be returned on
        ///     <see cref="ProducerResponse.Args"/>.
        /// </param>
        /// <returns>A Task&lt;ProducerResponse&gt; which can be awaited.</returns>
        public Task<ProducerResponse> MultiPublishAsync(string topic, IEnumerable<byte[]> bodies, params object[] args)
        {
            var doneChan = new Chan<ProducerResponse>();
            var cmd = Command.MultiPublish(topic, bodies);
            sendCommandAsync(cmd, doneChan, args);
            return Task.Factory.StartNew(() => doneChan.Receive());
        }

        /// <summary>
        ///     Synchronously publishes a message <paramref name="body"/> to the specified <paramref name="topic"/>, throwing
        ///     an exception if publish failed.
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="body">The message body.</param>
        public void Publish(string topic, byte[] body)
        {
            sendCommand(Command.Publish(topic, body));
        }

        /// <summary>
        ///     Synchronously publishes string <paramref name="value"/> message to the specified <paramref name="topic"/>,
        ///     throwing an exception if publish failed.
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="value">The message body.</param>
        public void Publish(string topic, string value)
        {
            Publish(topic, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        ///     Synchronously publishes a collection of message <paramref name="bodies"/> to the specified
        ///     <paramref name="topic"/>, throwing an exception if publish failed.
        /// </summary>
        /// <param name="topic">The topic to publish to.</param>
        /// <param name="bodies">The collection of message bodies.</param>
        public void MultiPublish(string topic, IEnumerable<byte[]> bodies)
        {
            var cmd = Command.MultiPublish(topic, bodies);
            sendCommand(cmd);
        }

        private void sendCommand(Command cmd)
        {
            var doneChan = new Chan<ProducerResponse>();

            try
            {
                sendCommandAsync(cmd, doneChan);
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

        private readonly Action<int> _throwErrStoppedAction = b => { throw new ErrStopped(); };

        private void sendCommandAsync(Command cmd, Chan<ProducerResponse> doneChan, params object[] args)
        {
            ProducerResponse t = null;
            try
            {
                if (_state != (int)State.Connected)
                {
                    Connect();
                }

                // keep track of how many outstanding producers we're dealing with
                // in order to later ensure that we clean them all up...
                Interlocked.Increment(ref _concurrentProducers);

                t = new ProducerResponse
                {
                    _cmd = cmd,
                    _doneChan = doneChan,
                    Args = args
                };

                Select
                    .CaseSend(_transactionChan, t)
                    .CaseReceive(_exitChan, _throwErrStoppedAction)
                    .NoDefault();

                Interlocked.Decrement(ref _concurrentProducers);
            }
            catch (Exception ex)
            {
                if (t != null)
                {
                    Interlocked.Decrement(ref _concurrentProducers);
                    t.Error = ex;
                    GoFunc.Run(() => t.finish(), "Producer: t.finish()");
                }
                else
                {
                    Thread.Sleep(1000); // slow down hammering Connect
                    throw;
                }
            }
        }

        /// <summary>
        ///     Connects to nsqd. Calling this method is optional; otherwise, Connect will be lazy invoked when Publish is
        ///     called.
        /// </summary>
        /// <exception cref="ErrStopped">Thrown if the Producer has been stopped.</exception>
        /// <exception cref="ErrNotConnected">Thrown if the Producer is currently waiting to close and reconnect.</exception>
        public void Connect()
        {
            lock (_guard)
            {
                if (_stopFlag == 1)
                    throw new ErrStopped();

                switch (_state)
                {
                    case (int)State.Init:
                        break;
                    case (int)State.Connected:
                        return;
                    default:
                        throw new ErrNotConnected();
                }

                log(Core.LogLevel.Info, string.Format("{0} connecting to nsqd", _addr));

                _conn = _connFactory(this);
                _conn.SetLogger(_logger, string.Format("P{0} ({{0}})", _id));
                try
                {
                    _conn.Connect();
                }
                catch (Exception ex)
                {
                    log(Core.LogLevel.Error, string.Format("({0}) error connecting to nsqd - {1}", _addr, ex.Message));
                    _conn.Close();
                    throw;
                }

                _state = (int)State.Connected;
                _closeChan = new Chan<int>();
                _wg.Add(1);
                log(Core.LogLevel.Info, string.Format("{0} connected to nsqd", _addr));
                GoFunc.Run(router, string.Format("Producer:router P{0}", _id));
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
            }, string.Format("Producer:close P{0}", _id));
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
                            log(Core.LogLevel.Error, string.Format("({0}) sending command - {1}", _conn, ex.Message));
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
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                while (doLoop)
                {
                    select.Execute();
                }
            }

            transactionCleanup();
            _wg.Done();
            log(Core.LogLevel.Info, "exiting router");
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
            var wg = new WaitGroup();
            wg.Add(_transactions.Count);
            foreach (var t in _transactions)
            {
                var t1 = t;
                GoFunc.Run(() =>
                           {
                               t1.Error = new ErrNotConnected();
                               t1.finish();
                               wg.Done();
                           }, "transactionCleanup: drain _transactions");
            }
            _transactions.Clear();

            // spin and free up any writes that might have raced
            // with the cleanup process (blocked on writing
            // to transactionChan)

            // give the runtime a chance to schedule other racing goroutines
            var ticker = new Ticker(TimeSpan.FromMilliseconds(100));
            bool doLoop = true;
            using (var select =
                    Select
                    .CaseReceive(_transactionChan, t =>
                    {
                        wg.Add(1);
                        GoFunc.Run(() =>
                                   {
                                       t.Error = new ErrNotConnected();
                                       t.finish();
                                       wg.Done();
                                   }, "transactionCleanup: finish transaction from _transactionChan");
                    })
                    .CaseReceive(ticker.C, _ =>
                    {
                        // keep spinning until there are 0 concurrent producers
                        if (_concurrentProducers == 0)
                        {
                            doLoop = false;
                            return;
                        }
                        log(Core.LogLevel.Warning, string.Format(
                            "waiting for {0} concurrent producers to finish", _concurrentProducers));
                    })
                    .NoDefault(defer: true)
            )
            {
                while (doLoop)
                {
                    select.Execute();
                }
            }
            ticker.Close();

            wg.Wait();
        }

        private void log(Core.LogLevel lvl, string line)
        {
            // TODO: proper width formatting
            _logger.Output(lvl, string.Format("P{0} {1}", _id, line));
        }

        void IConnDelegate.OnResponse(Conn c, byte[] data)
        {
            _responseChan.Send(data);
        }

        void IConnDelegate.OnError(Conn c, byte[] data)
        {
            _errorChan.Send(data);
        }

        void IConnDelegate.OnIOError(Conn c, Exception err)
        {
            close();
        }

        void IConnDelegate.OnClose(Conn c)
        {
            lock (_guard)
            {
                _closeChan.Close();
            }
        }

        void IConnDelegate.OnMessage(Conn c, Message m) { }
        void IConnDelegate.OnMessageFinished(Conn c, Message m) { }
        void IConnDelegate.OnMessageRequeued(Conn c, Message m) { }
        void IConnDelegate.OnBackoff(Conn c) { }
        void IConnDelegate.OnContinue(Conn c) { }
        void IConnDelegate.OnResume(Conn c) { }
        void IConnDelegate.OnHeartbeat(Conn c) { }
    }
}
