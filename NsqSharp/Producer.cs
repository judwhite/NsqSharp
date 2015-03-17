using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Loggers;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/master/producer.go

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
        void SetLogger(ILogger l, string format);

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
    public partial class Producer
    {
        internal long _id;
        private readonly string _addr;
        private IConn _conn;
        private readonly Config _config;

        private readonly ILogger _logger;

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
    internal class ProducerTransaction
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
            : this(nsqdAddress, new ConsoleLogger(LogLevel.Info), config, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="nsqdAddress">The nsqd address.</param>
        /// <param name="logger">The logger.</param>
        public Producer(string nsqdAddress, ILogger logger)
            : this(nsqdAddress, logger, new Config(), null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="nsqdAddress">The nsqd address.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The config. After Config is passed into NewProducer the values are
        /// no longer mutable (they are copied).</param>
        public Producer(string nsqdAddress, ILogger logger, Config config)
            : this(nsqdAddress, logger, config, null)
        {
        }

        private Producer(string addr, ILogger logger, Config config, Func<Producer, IConn> connFactory)
        {
            if (string.IsNullOrEmpty(addr))
                throw new ArgumentNullException("addr");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (config == null)
                throw new ArgumentNullException("config");

            _id = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds % 10000; // TODO: Remove

            config.Validate();

            _addr = addr;
            _config = config.Clone();

            _logger = logger;

            _transactionChan = new Chan<ProducerTransaction>();
            _exitChan = new Chan<int>();
            _responseChan = new Chan<byte[]>();
            _errorChan = new Chan<byte[]>();

            if (connFactory == null)
                connFactory = p => new Conn(_addr, _config, p);

            _connFactory = connFactory;
        }

        // TODO: This code really isn't useful.
        /*/// <summary>
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
        }*/

        /// <summary>
        /// String returns the address of the Producer.
        /// </summary>
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
        /// Publish synchronously publishes a message body to the specified topic, throwing
        /// an exception if publish failed.
        /// </summary>
        public void Publish(string topic, byte[] body)
        {
            sendCommand(Command.Publish(topic, body));
        }

        /// <summary>
        /// Publish synchronously publishes a string to the specified topic, throwing
        /// an exception if publish failed.
        /// </summary>
        public void Publish(string topic, string value)
        {
            Publish(topic, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// MultiPublish synchronously publishes a collection of message bodies to the specified topic, throwing
        /// an exception if publish failed.
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

            var t = new ProducerTransaction
            {
                _cmd = cmd,
                _doneChan = doneChan,
                Args = args,
            };

            try
            {
                if (_state != (int)State.Connected)
                {
                    Connect();
                }

                Select
                    .CaseSend(_transactionChan, t, () => { })
                    .CaseReceive(_exitChan, m => { throw new ErrStopped(); })
                    .NoDefault();
            }
            catch (Exception ex)
            {
                t.Error = ex;
                doneChan.Send(t);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentProducers);
            }
        }

        /// <summary>
        /// Connects to nsqd. Calling this method is optional; otherwise, Connect will
        /// be lazy invoked when Publish is called.
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

                log(LogLevel.Info, string.Format("{0} connecting to nsqd", _addr));

                _conn = _connFactory(this);
                _conn.SetLogger(_logger, string.Format("P{0} ({{0}})", _id));
                try
                {
                    _conn.Connect();
                }
                catch (Exception ex)
                {
                    _conn.Close();
                    log(LogLevel.Error, string.Format("({0}) error connecting to nsqd - {1}", _addr, ex.Message));
                    _state = (int)State.Init;
                    throw;
                }

                _closeChan = new Chan<int>();
                _wg.Add(1);
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
                            log(LogLevel.Error, string.Format("({0}) sending command - {1}", _conn, ex.Message));
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
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
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

        private void log(LogLevel lvl, string line)
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
