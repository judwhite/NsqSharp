using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NsqSharp.Channels;
using NsqSharp.Go;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/producer.go

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
        private Conn _conn;
        private readonly Config _config;

        private ILogger _logger;
        private LogLevel _logLvl;

        private readonly Chan<byte[]> _responseChan;
        private readonly Chan<byte[]> _errorChan;
        private Chan<int> _closeChan;

        private readonly Chan<ProducerTransaction> _transactionChan;
        private readonly List<ProducerTransaction> _transactions = new List<ProducerTransaction>();
        private int _state;

        private int _concurrentProducers;
        private int _stopFlag;
        private readonly Chan<int> _exitChan;
        private readonly WaitGroup _wg = new WaitGroup();
        private readonly object _guard = new object();
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

    public partial class Producer
    {
        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="addr">The address.</param>
        /// <param name="config">The config. After Config is passed into NewProducer the values are
        /// no longer mutable (they are copied).</param>
        public Producer(string addr, Config config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _id = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds % 10000; // TODO: Remove

            config.Validate();

            _addr = addr;
            _config = config.Clone();

            _logger = new Logger(); // TODO
            _logLvl = LogLevel.Info;

            _transactionChan = new Chan<ProducerTransaction>();
            _exitChan = new Chan<int>();
            _responseChan = new Chan<byte[]>();
            _errorChan = new Chan<byte[]>();
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
        /// <param name="l">The <see cref="Logger"/></param>
        /// <param name="lvl">The <see cref="LogLevel"/></param>
        public void SetLogger(Logger l, LogLevel lvl)
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
        public void PublishAsync(string topic, byte[] body, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            sendCommandAsync(Command.Publish(topic, body), doneChan, args);
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
        public void MultiPublishAsync(string topic, List<byte[]> body, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            var cmd = Command.MultiPublish(topic, body);
            sendCommandAsync(cmd, doneChan, args);
        }

        /// <summary>
        /// Publish synchronously publishes a message body to the specified topic, returning
        /// the an error if publish failed
        /// </summary>
        public void Publish(string topic, byte[] body)
        {
            sendCommand(Command.Publish(topic, body));
        }

        /// <summary>
        /// MultiPublish synchronously publishes a slice of message bodies to the specified topic, returning
        /// the an error if publish failed
        /// </summary>
        public void MultiPublish(string topic, List<byte[]> body)
        {
            var cmd = Command.MultiPublish(topic, body);
            sendCommand(cmd);
        }

        private void sendCommand(Command cmd)
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

            doneChan.Receive();
        }

        private void sendCommandAsync(Command cmd, Chan<ProducerTransaction> doneChan, params object[] args)
        {
            Interlocked.Increment(ref _concurrentProducers);
            try
            {
                if (_state != (int)State.Connected)
                {
                    connect();
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

        private void connect()
        {
            lock (_guard)
            {
                if (_stopFlag == 1)
                    throw new ErrStopped();

                const int newValue = (int)State.Connected;
                const int comparand = (int)State.Init;
                if (Interlocked.CompareExchange(ref _state, newValue, comparand) != comparand)
                {
                    throw new ErrNotConnected();
                }

                log(LogLevel.Info, "{0} connecting to nsqd", _addr);

                _conn = new Conn(_addr, _config, new ProducerConnDelegate { w = this });
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
            while (doLoop)
            {
                Select
                    .CaseReceive(_transactionChan, t =>
                    {
                        _transactions.Add(t);
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
                    .NoDefault();
            }

            transactionCleanup();
            _wg.Done();
            log(LogLevel.Info, "exiting router");
        }

        private void popTransaction(FrameType frameType, byte[] data)
        {
            var t = _transactions[0];
            _transactions.RemoveAt(0);
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
            // TODO: Create PR on go-nsq, possible race condition on w.logger
            var logger = _logger;
            if (logger == null)
                return;

            if (_logLvl > lvl)
                return;

            logger.Output(2, string.Format("{0} {1} {2}", Log.Prefix(lvl), _id, string.Format(line, args)));
        }

        internal void onConnResponse(Conn c, byte[] data) { _responseChan.Send(data); }
        internal void onConnError(Conn c, byte[] data) { _errorChan.Send(data); }
        internal void onConnHeartbeat(Conn c) { }
        internal void onConnIOError(Conn c, Exception err) { close(); }
        internal void onConnClose(Conn c)
        {
            lock (_guard)
            {
                _closeChan.Close();
            }
        }
    }
}
