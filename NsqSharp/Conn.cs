using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Channels;
using NsqSharp.Extensions;
using NsqSharp.Go;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/conn.go

    /// <summary>
    /// IdentifyResponse represents the metadata
    /// returned from an IDENTIFY command to nsqd
    /// </summary>
    public class IdentifyResponse
    {
        /// <summary>Max RDY count</summary>
        [JsonProperty("max_rdy_count")]
        public long MaxRdyCount { get; set; }
        /// <summary>Use TLSv1</summary>
        [JsonProperty("tls_v1")]
        public bool TLSv1 { get; set; }
        /// <summary>Use Deflate compression</summary>
        [JsonProperty("deflate")]
        public bool Deflate { get; set; }
        /// <summary>Use Snappy compression</summary>
        [JsonProperty("snappy")]
        public bool Snappy { get; set; }
        /// <summary>Auth required</summary>
        [JsonProperty("auth_required")]
        public bool AuthRequired { get; set; }
    }

    /// <summary>
    /// AuthResponse represents the metadata
    /// returned from an AUTH command to nsqd
    /// </summary>
    public class AuthResponse
    {
        /// <summary>Identity</summary>
        [JsonProperty("identity")]
        public string Identity { get; set; }
        /// <summary>Identity URL</summary>
        [JsonProperty("identity_url")]
        public string IdentityUrl { get; set; }
        /// <summary>Permission Count</summary>
        [JsonProperty("permission_count")]
        public long PermissionCount { get; set; }
    }

    internal class msgResponse
    {
        public Message msg { get; set; }
        public Command cmd { get; set; }
        public bool success { get; set; }
        public bool backoff { get; set; }
    }

    /// <summary>
    /// Conn represents a connection to nsqd
    ///
    /// Conn exposes a set of callbacks for the
    /// various events that occur on a connection
    /// </summary>
    public partial class Conn : IReader, IWriter
    {
        private static readonly byte[] HEARTBEAT_BYTES = Encoding.UTF8.GetBytes("_heartbeat_");

        private long _messagesInFlight;
        private long _maxRdyCount;
        private long _rdyCount;
        private long _lastRdyCount;
        private long _lastMsgTimestamp;

        private readonly object _mtx = new object();

        private readonly Config _config;

        private ITcpConn _conn;
        // TODO: tlsConn
        private readonly string _addr;

        private readonly IConnDelegate _delegate;

        private ILogger _logger;
        private LogLevel _logLvl;
        private string _logFmt;

        private IReader _r;
        private IWriter _w;

        // TODO: create PR for go-nsq to remove unused fields
        // https://github.com/bitly/go-nsq/blob/103a1c5b3b6acbe9dd6c8eeb2ad7fb47f6863fc4/conn.go#L75
        // private int _backoffCounter;
        // rdyRetryTimer *time.Timer

        private readonly Chan<Command> _cmdChan;
        private readonly Chan<msgResponse> _msgResponseChan;
        private readonly Chan<int> _exitChan;
        private readonly Chan<int> _drainReady;

        private int _closeFlag;
        private readonly Once _stopper = new Once();
        private readonly WaitGroup _wg = new WaitGroup();
        private int _readLoopRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="Conn"/> class.
        /// </summary>
        public Conn(string addr, Config config, IConnDelegate connDelegate)
        {
            if (string.IsNullOrEmpty(addr))
                throw new ArgumentNullException("addr");
            if (config == null)
                throw new ArgumentNullException("config");
            if (connDelegate == null)
                throw new ArgumentNullException("connDelegate");

            _addr = addr;

            _config = config.Clone();
            _delegate = connDelegate;

            _maxRdyCount = 2500;
            _lastMsgTimestamp = DateTime.Now.UnixNano();

            _cmdChan = new Chan<Command>();
            _msgResponseChan = new Chan<msgResponse>();
            _exitChan = new Chan<int>();
            _drainReady = new Chan<int>();
        }

        /// <summary>
        /// SetLogger assigns the logger to use as well as a level.
        ///
        /// The format parameter is expected to be a printf compatible string with
        /// a single {0} argument.  This is useful if you want to provide additional
        /// context to the log messages that the connection will print, the default
        /// is '({0})'.
        ///
        /// The logger parameter is an interface that requires the following
        /// method to be implemented (such as the the stdlib log.Logger):
        ///
        ///    Output(int calldepth, string s)
        ///
        /// </summary>
        public void SetLogger(ILogger l, LogLevel lvl, string format)
        {
            if (l == null)
                throw new ArgumentNullException("l");

            _logger = l;
            _logLvl = lvl;
            _logFmt = format;
            if (string.IsNullOrWhiteSpace(_logFmt))
            {
                _logFmt = "({0})";
            }
        }

        /// <summary>
        /// Connect dials and bootstraps the nsqd connection
        /// (including IDENTIFY) and returns the IdentifyResponse
        /// </summary>
        public IdentifyResponse Connect()
        {
            var conn = Net.DialTimeout("tcp", _addr, TimeSpan.FromSeconds(1));
            _conn = (ITcpConn)conn;
            _r = conn;
            _w = conn;

            try
            {
                Write(Protocol.MagicV2);
            }
            catch (Exception ex)
            {
                _conn.Close();
                throw new Exception(string.Format("[{0}] failed to write magic - {1}", _addr, ex.Message), ex);
            }

            var resp = identify();

            if (resp != null && resp.AuthRequired)
            {
                if (string.IsNullOrEmpty(_config.AuthSecret))
                {
                    log(LogLevel.Error, "Auth Required");
                    throw new Exception("Auth Required");
                }
                auth(_config.AuthSecret);
            }

            _wg.Add(2);
            _readLoopRunning = 1;
            GoFunc.Run(readLoop);
            GoFunc.Run(writeLoop);
            return resp;
        }

        /// <summary>
        /// Close idempotently initiates connection close
        /// </summary>
        public void Close()
        {
            _closeFlag = 1;
            if (_conn != null && _messagesInFlight == 0)
            {
                _conn.CloseRead();
            }
        }

        /// <summary>
        /// IsClosing indicates whether or not the
        /// connection is currently in the processing of
        /// gracefully closing
        /// </summary>
        public bool IsClosing
        {
            get { return (_closeFlag == 1); }
        }

        /// <summary>
        /// RDY returns the current RDY count
        /// </summary>
        public long RDY
        {
            get { return _rdyCount; }
        }

        /// <summary>
        /// LastRDY returns the previously set RDY count
        /// </summary>
        public long LastRDY
        {
            get { return _lastRdyCount; }
        }

        /// <summary>
        /// SetRDY stores the specified RDY count
        /// </summary>
        public void SetRDY(long rdy)
        {
            // TODO: Should this be in lock to sync?
            _rdyCount = rdy;
            _lastRdyCount = rdy;
        }

        /// <summary>
        /// MaxRDY returns the nsqd negotiated maximum
        /// RDY count that it will accept for this connection
        /// </summary>
        public long MaxRDY
        {
            get { return _maxRdyCount; }
        }

        /// <summary>
        /// LastMessageTime returns a time.Time representing
        /// the time at which the last message was received
        /// </summary>
        public DateTime LastMessageTime
        {
            get { return Time.Unix(0, _lastMsgTimestamp); }
        }

        /// <summary>
        /// RemoteAddr returns the configured destination nsqd address
        /// </summary>
        public string RemoteAddr()
        {
            //return _conn.RemoteAddr(); // TODO
            return _addr;
        }

        /// <summary>
        /// String returns the fully-qualified address
        /// </summary>
        public override string ToString()
        {
            return _addr;
        }

        /// <summary>
        /// Read performs a deadlined read on the underlying TCP connection
        /// </summary>
        public int Read(byte[] p)
        {
            //c.conn.SetReadDeadline(time.Now().Add(c.config.ReadTimeout)) // TODO
            return _r.Read(p);
        }

        /// <summary>
        /// Write performs a deadlined write on the underlying TCP connection
        /// </summary>
        public int Write(byte[] p)
        {
            //c.conn.SetWriteDeadline(time.Now().Add(c.config.WriteTimeout)) // TODO
            return _w.Write(p);
        }

        /// <summary>
        /// WriteCommand is a goroutine safe method to write a Command
        /// to this connection, and flush.
        /// </summary>
        public void WriteCommand(Command cmd)
        {
            try
            {
                lock (_mtx)
                {
                    cmd.WriteTo(this);
                    Flush();
                }
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, "IO error - {0}", ex.Message);
                _delegate.OnIOError(this, ex);
                throw;
            }
        }
    }

    internal interface IFlusher
    {
        void Flush();
    }

    public partial class Conn
    {
        /// <summary>
        /// Flush writes all buffered data to the underlying TCP connection
        /// </summary>
        public void Flush()
        {
            // TODO: no-op for now. TcpClient doesn't nicely break apart read/write streams; may need to use Sockets
        }

        private IdentifyResponse identify()
        {
            var ci = new Dictionary<string, object>();
            ci["client_id"] = _config.ClientID;
            ci["hostname"] = _config.Hostname;
            ci["user_agent"] = _config.UserAgent;
            ci["short_id"] = _config.ClientID; // deprecated
            ci["long_id"] = _config.Hostname;  // deprecated
            ci["tls_v1"] = _config.TlsV1;
            ci["deflate"] = _config.Deflate;
            ci["deflate_level"] = _config.DeflateLevel;
            ci["snappy"] = _config.Snappy;
            ci["feature_negotiation"] = true;
            if (_config.HeartbeatInterval <= TimeSpan.Zero)
            {
                ci["heartbeat_interval"] = -1;
            }
            else
            {
                ci["heartbeat_interval"] = (int)_config.HeartbeatInterval.TotalMilliseconds;
            }
            ci["sample_rate"] = _config.SampleRate;
            ci["output_buffer_size"] = _config.OutputBufferSize;
            if (_config.OutputBufferTimeout <= TimeSpan.Zero)
            {
                ci["output_buffer_timeout"] = -1;
            }
            else
            {
                ci["output_buffer_timeout"] = (int)_config.OutputBufferTimeout.TotalMilliseconds;
            }
            ci["msg_timeout"] = (int)_config.MsgTimeout.TotalMilliseconds;

            Command cmd;
            try
            {
                cmd = Command.Identify(ci);
                WriteCommand(cmd);

                FrameType frameType;
                byte[] data;
                Protocol.ReadUnpackedResponse(this, out frameType, out data);

                string json = Encoding.UTF8.GetString(data);

                if (frameType == FrameType.Error)
                {
                    throw new ErrIdentify(json);
                }

                // check to see if the server was able to respond w/ capabilities
                // i.e. it was a JSON response
                if (data[0] != '{')
                {
                    return null;
                }

                var resp = JsonConvert.DeserializeObject<IdentifyResponse>(Encoding.UTF8.GetString(data));

                log(LogLevel.Debug, "IDENTIFY response: {0}", resp);

                _maxRdyCount = resp.MaxRdyCount;

                /*if resp.TLSv1 {
                    c.log(LogLevelInfo, "upgrading to TLS")
                    err := c.upgradeTLS(c.config.TlsConfig)
                    if err != nil {
                        return nil, ErrIdentify{err.Error()}
                    }
                }*/

                /*if resp.Deflate {
                    c.log(LogLevelInfo, "upgrading to Deflate")
                    err := c.upgradeDeflate(c.config.DeflateLevel)
                    if err != nil {
                        return nil, ErrIdentify{err.Error()}
                    }
                }*/

                /*if resp.Snappy {
                    c.log(LogLevelInfo, "upgrading to Snappy")
                    err := c.upgradeSnappy()
                    if err != nil {
                        return nil, ErrIdentify{err.Error()}
                    }
                }*/

                // now that connection is bootstrapped, enable read buffering
                // (and write buffering if it's not already capable of Flush())

                // TODO: Determine if TcpClient or Socket should be used, and what needs to be done about buffering
                /*c.r = bufio.NewReader(c.r)
                if _, ok := c.w.(flusher); !ok {
                    c.w = bufio.NewWriter(c.w)
                }*/

                return resp;
            }
            catch (Exception ex)
            {
                throw new ErrIdentify(ex.Message, ex);
            }
        }

        private void upgradeTLS()
        {
            // TODO
        }

        private void upgradeDeflate()
        {
            // TODO
        }

        private void upgradeSnappy()
        {
            // TODO
        }

        private void auth(string secret)
        {
            var cmd = Command.Auth(secret);

            WriteCommand(cmd);

            FrameType frameType;
            byte[] data;
            Protocol.ReadUnpackedResponse(this, out frameType, out data);

            string json = Encoding.UTF8.GetString(data);

            if (frameType == FrameType.Error)
            {
                throw new Exception(string.Format("Error authenticating {0}", json));
            }

            var resp = JsonConvert.DeserializeObject<AuthResponse>(json);

            log(LogLevel.Info, "Auth accepted. Identity: {0} {1} Permissions: {2}",
                resp.Identity, resp.IdentityUrl, resp.PermissionCount);
        }

        private void readLoop()
        {
            try
            {
                var msgDelegate = new ConnMessageDelegate { c = this };

                bool doLoop = true;
                while (doLoop)
                {
                    if (_closeFlag == 1)
                    {
                        break;
                    }

                    FrameType frameType;
                    byte[] data;
                    try
                    {
                        Protocol.ReadUnpackedResponse(this, out frameType, out data);
                    }
                    catch (Exception ex)
                    {
                        // TODO: determine equivalent exception type from .NET runtime
                        if (!ex.Message.Contains("use of closed network connection"))
                        {
                            log(LogLevel.Error, "IO error - {0}", ex.Message);
                            _delegate.OnIOError(this, ex);
                        }
                        break;
                    }

                    if (frameType == FrameType.Response && Bytes.Equal(data, HEARTBEAT_BYTES))
                    {
                        _delegate.OnHeartbeat(this);
                        try
                        {
                            WriteCommand(Command.Nop());
                        }
                        catch (Exception ex)
                        {
                            log(LogLevel.Error, "IO error - {0}", ex.Message);
                            _delegate.OnIOError(this, ex);
                            break;
                        }
                        continue;
                    }

                    switch (frameType)
                    {
                        case FrameType.Response:
                            _delegate.OnResponse(this, data);
                            break;
                        case FrameType.Message:
                            Message msg;
                            try
                            {
                                msg = Message.DecodeMessage(data);
                            }
                            catch (Exception ex)
                            {
                                log(LogLevel.Error, "IO error - {0}", ex.Message);
                                _delegate.OnIOError(this, ex);
                                doLoop = false;
                                break;
                            }
                            msg.Delegate = msgDelegate;
                            msg.NSQDAddress = ToString();

                            Interlocked.Decrement(ref _rdyCount);
                            Interlocked.Increment(ref _messagesInFlight);
                            _lastMsgTimestamp = DateTime.Now.UnixNano();

                            _delegate.OnMessage(this, msg);
                            break;
                        case FrameType.Error:
                            log(LogLevel.Error, "protocol error - {0}", data); // TODO: this is a byte array...
                            _delegate.OnError(this, data);
                            break;
                        default:
                            // TODO: what would 'err' be in this case?
                            // https://github.com/bitly/go-nsq/blob/v1.0.2/conn.go#L510
                            log(LogLevel.Error, "IO error");
                            _delegate.OnIOError(this, new Exception(string.Format("unknown frame type {0}", frameType)));
                            break;
                    }
                }
            }
            finally
            {
                //exit:
                _readLoopRunning = 0;
                var messagesInFlight = _messagesInFlight;
                if (messagesInFlight == 0)
                {
                    // if we exited readLoop with no messages in flight
                    // we need to explicitly trigger the close because
                    // writeLoop won't
                    close();
                }
                else
                {
                    log(LogLevel.Warning, "delaying close, {0} outstanding messages", messagesInFlight);
                }
                _wg.Done();
                log(LogLevel.Info, "readLoop exiting");
            }
        }

        private void writeLoop()
        {
            bool doLoop = true;
            while (doLoop)
            {
                Select
                    .CaseReceive(_exitChan, o =>
                    {
                        log(LogLevel.Info, "breaking out of writeLoop");
                        // Indicate drainReady because we will not pull any more off msgResponseChan
                        _drainReady.Close();
                        doLoop = false;
                    })
                    .CaseReceive(_cmdChan, cmd =>
                    {
                        try
                        {
                            WriteCommand(cmd);
                        }
                        catch (Exception ex)
                        {
                            log(LogLevel.Error, "error sending command {0} - {1}", cmd, ex.Message);
                            close();
                            // TODO: Create PR to remove unnecessary continue in go-nsq
                            // https://github.com/bitly/go-nsq/blob/v1.0.2/conn.go#L544
                        }
                    })
                    .CaseReceive(_msgResponseChan, resp =>
                    {
                        // Decrement this here so it is correct even if we can't respond to nsqd
                        var msgsInFlight = Interlocked.Decrement(ref _messagesInFlight);

                        if (resp.success)
                        {
                            log(LogLevel.Debug, "FIN {0}", resp.msg.ID);
                            _delegate.OnMessageFinished(this, resp.msg);
                            if (resp.backoff)
                            {
                                _delegate.OnResume(this);
                            }
                        }
                        else
                        {
                            log(LogLevel.Debug, "REQ {0}", resp.msg.ID);
                            _delegate.OnMessageRequeued(this, resp.msg);
                            if (resp.backoff)
                            {
                                _delegate.OnBackoff(this);
                            }
                        }

                        try
                        {
                            WriteCommand(resp.cmd);

                            if (msgsInFlight == 0 && _closeFlag == 1)
                            {
                                close();
                            }
                        }
                        catch (Exception ex)
                        {
                            log(LogLevel.Error, "error sending command {0} - {1}", resp.cmd, ex);
                            close();
                        }
                    })
                    .NoDefault();
            }

            _wg.Done();
            log(LogLevel.Info, "writeLoop exiting");
        }

        private void close()
        {
            // a "clean" connection close is orchestrated as follows:
            //
            //     1. CLOSE cmd sent to nsqd
            //     2. CLOSE_WAIT response received from nsqd
            //     3. set c.closeFlag
            //     4. readLoop() exits
            //         a. if messages-in-flight > 0 delay close()
            //             i. writeLoop() continues receiving on c.msgResponseChan chan
            //                 x. when messages-in-flight == 0 call close()
            //         b. else call close() immediately
            //     5. c.exitChan close
            //         a. writeLoop() exits
            //             i. c.drainReady close
            //     6a. launch cleanup() goroutine (we're racing with intraprocess
            //        routed messages, see comments below)
            //         a. wait on c.drainReady
            //         b. loop and receive on c.msgResponseChan chan
            //            until messages-in-flight == 0
            //            i. ensure that readLoop has exited
            //     6b. launch waitForCleanup() goroutine
            //         b. wait on waitgroup (covers readLoop() and writeLoop()
            //            and cleanup goroutine)
            //         c. underlying TCP connection close
            //         d. trigger Delegate OnClose()
            //

            _stopper.Do(() =>
            {
                log(LogLevel.Info, "beginning close");
                _exitChan.Close();
                _conn.CloseRead();

                _wg.Add(1);
                GoFunc.Run(cleanup);

                GoFunc.Run(waitForCleanup);
            });
        }

        private void cleanup()
        {
            _drainReady.Receive();
            var ticker = new Ticker(TimeSpan.FromMilliseconds(100));
            var lastWarning = DateTime.Now;

            // writeLoop has exited, drain any remaining in flight messages
            while (true)
            {
                // TODO: Review how reading off _msgResponseChan could impact

                // we're racing with readLoop which potentially has a message
                // for handling so infinitely loop until messagesInFlight == 0
                // and readLoop has exited
                long msgsInFlight = _messagesInFlight;

                Select
                    .CaseReceive(_msgResponseChan, o => msgsInFlight = Interlocked.Decrement(ref _messagesInFlight))
                    .CaseReceive(ticker.C, o => msgsInFlight = _messagesInFlight)
                    .NoDefault();

                if (msgsInFlight > 0)
                {
                    if (DateTime.Now - lastWarning > TimeSpan.FromSeconds(1))
                    {
                        log(LogLevel.Warning, "draining... waiting for {0} messages in flight", msgsInFlight);
                        lastWarning = DateTime.Now;
                    }
                    continue;
                }

                // until the readLoop has exited we cannot be sure that there
                // still won't be a race
                if (_readLoopRunning == 1)
                {
                    if (DateTime.Now - lastWarning > TimeSpan.FromSeconds(1))
                    {
                        log(LogLevel.Warning, "draining... readLoop still running");
                        lastWarning = DateTime.Now;
                    }
                    continue;
                }
                break;
            }

            //exit:
            ticker.Stop();
            _wg.Done();
            log(LogLevel.Info, "finished draining, cleanup exiting");
        }

        private void waitForCleanup()
        {
            _wg.Wait();
            _conn.CloseWrite();
            log(LogLevel.Info, "clean close complete");
            _delegate.OnClose(this);
        }

        internal void onMessageFinish(Message m)
        {
            // TODO: backoff true? https://github.com/bitly/go-nsq/blob/v1.0.2/conn.go#L673
            _msgResponseChan.Send(new msgResponse { msg = m, cmd = Command.Finish(m.ID), success = true, backoff = true });
        }

        internal void onMessageRequeue(Message m, TimeSpan? delay, bool backoff)
        {
            if (delay == null || delay <= TimeSpan.Zero)
            {
                // linear delay
                delay = TimeSpan.FromTicks(_config.DefaultRequeueDelay.Ticks * m.Attempts);
                // bound the requeueDelay to configured max
                if (delay > _config.MaxRequeueDelay)
                {
                    delay = _config.MaxRequeueDelay;
                }
            }

            _msgResponseChan.Send(new msgResponse { msg = m, cmd = Command.Requeue(m.ID, delay.Value), success = false, 
                backoff = backoff });
        }

        internal void onMessageTouch(Message m)
        {
            Select
                .CaseSend(_cmdChan, Command.Touch(m.ID), () => { })
                .CaseReceive(_exitChan, o => { })
                .NoDefault();
        }

        private void log(LogLevel lvl, string line, params object[] args)
        {
            // TODO: thread safety

            if (_logger == null)
                return;

            if (_logLvl > lvl)
            {
                return;
            }

            // TODO: Review format string
            _logger.Output(2, string.Format("{0} {1} {2}", Log.Prefix(lvl),
                string.Format(_logFmt, ToString()),
                string.Format(line, args)));
        }
    }
}
