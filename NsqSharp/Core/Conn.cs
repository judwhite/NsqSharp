using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Extensions;

namespace NsqSharp.Core
{
    // https://github.com/bitly/go-nsq/blob/master/conn.go

    /// <summary>
    /// IdentifyResponse represents the metadata
    /// returned from an IDENTIFY command to nsqd
    /// </summary>
    [DataContract]
    public class IdentifyResponse
    {
        /// <summary>Max RDY count</summary>
        [DataMember(Name = "max_rdy_count")]
        public long MaxRdyCount { get; set; }
        /// <summary>Use TLSv1</summary>
        [DataMember(Name = "tls_v1")]
        public bool TLSv1 { get; set; }
        /// <summary>Use Deflate compression</summary>
        [DataMember(Name = "deflate")]
        public bool Deflate { get; set; }
        /// <summary>Use Snappy compression</summary>
        [DataMember(Name = "snappy")]
        public bool Snappy { get; set; }
        /// <summary>Auth required</summary>
        [DataMember(Name = "auth_required")]
        public bool AuthRequired { get; set; }
    }

    /// <summary>
    /// AuthResponse represents the metadata
    /// returned from an AUTH command to nsqd
    /// </summary>
    [DataContract]
    public class AuthResponse
    {
        /// <summary>Identity</summary>
        [DataMember(Name = "identity")]
        public string Identity { get; set; }
        /// <summary>Identity URL</summary>
        [DataMember(Name = "identity_url")]
        public string IdentityUrl { get; set; }
        /// <summary>Permission Count</summary>
        [DataMember(Name = "permission_count")]
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
    public partial class Conn : IReader, IWriter, IConn
    {
        private static readonly byte[] HEARTBEAT_BYTES = Encoding.UTF8.GetBytes("_heartbeat_");

        internal long _messagesInFlight;
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
        private string _logFmt;

        private IReader _r;
        private IWriter _w;

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
        /// </summary>
        public void SetLogger(ILogger l, string format)
        {
            if (l == null)
                throw new ArgumentNullException("l");

            _logger = l;
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
            if (_conn == null)
                throw new Exception("Net.DialTimeout returned null");
            _r = conn;
            _w = conn;

            _conn.ReadTimeout = _config.ReadTimeout;
            _conn.WriteTimeout = _config.WriteTimeout;

            try
            {
                Write(Protocol.MagicV2, 0, Protocol.MagicV2.Length);
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
            // SetReadDeadline handled in Connect
            return _r.Read(p);
        }

        /// <summary>
        /// Write performs a deadlined write on the underlying TCP connection
        /// </summary>
        public int Write(byte[] p, int offset, int length)
        {
            // SetWriteDeadline handled in Connect
            return _w.Write(p, offset, length);
        }

        private int _bigBufSize = 4096;
        private byte[] _bigBuf = new byte[4096];

        /// <summary>
        /// WriteCommand is a thread safe method to write a Command
        /// to this connection, and flush.
        /// </summary>
        public void WriteCommand(Command cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            try
            {
                lock (_mtx)
                {
                    int size = cmd.GetByteCount();
                    if (size > _bigBufSize)
                    {
                        _bigBuf = new byte[size];
                        _bigBufSize = size;
                    }

                    cmd.WriteTo(this, _bigBuf);

                    Flush();
                }
            }
            catch (Exception ex)
            {
                log(LogLevel.Error, string.Format("IO error - {0}", ex));
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
            _conn.Flush();
        }

        private IdentifyResponse identify()
        {
            var ci = new IdentifyRequest();
            ci.client_id = _config.ClientID;
            ci.hostname = _config.Hostname;
            ci.user_agent = _config.UserAgent;
            ci.short_id = _config.ClientID; // deprecated
            ci.long_id = _config.Hostname;  // deprecated
            ci.tls_v1 = _config.TlsV1;
            ci.deflate = _config.Deflate;
            ci.deflate_level = _config.DeflateLevel;
            ci.snappy = _config.Snappy;
            ci.feature_negotiation = true;
            if (_config.HeartbeatInterval <= TimeSpan.Zero)
            {
                ci.heartbeat_interval = -1;
            }
            else
            {
                ci.heartbeat_interval = (int)_config.HeartbeatInterval.TotalMilliseconds;
            }
            ci.sample_rate = _config.SampleRate;
            ci.output_buffer_size = _config.OutputBufferSize;
            if (_config.OutputBufferTimeout <= TimeSpan.Zero)
            {
                ci.output_buffer_timeout = -1;
            }
            else
            {
                ci.output_buffer_timeout = (int)_config.OutputBufferTimeout.TotalMilliseconds;
            }
            ci.msg_timeout = (int)_config.MsgTimeout.TotalMilliseconds;

            try
            {
                var cmd = Command.Identify(ci);
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

                string respJson = Encoding.UTF8.GetString(data);
                log(LogLevel.Debug, string.Format("IDENTIFY response: {0}", respJson));

                IdentifyResponse resp;
                var serializer = new DataContractJsonSerializer(typeof(IdentifyResponse));
                using (var memoryStream = new MemoryStream(data))
                {
                    resp = (IdentifyResponse)serializer.ReadObject(memoryStream);
                }

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
            catch (ErrIdentify)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ErrIdentify(ex.Message, ex);
            }
        }

        /*private void upgradeTLS()
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
        }*/

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

            AuthResponse resp;
            var serializer = new DataContractJsonSerializer(typeof(AuthResponse));
            using (var memoryStream = new MemoryStream(data))
            {
                resp = (AuthResponse)serializer.ReadObject(memoryStream);
            }

            log(LogLevel.Info, string.Format("Auth accepted. Identity: {0} {1} Permissions: {2}",
                resp.Identity, resp.IdentityUrl, resp.PermissionCount));
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
                        // if !strings.Contains(err.Error(), "use of closed network connection")
                        if (_closeFlag != 1)
                        {
                            log(LogLevel.Error, string.Format("IO error - {0}", ex.Message));
                            _delegate.OnIOError(this, ex);
                        }
                        break;
                    }

                    if (frameType == FrameType.Response && HEARTBEAT_BYTES.SequenceEqual(data))
                    {
                        _delegate.OnHeartbeat(this);
                        try
                        {
                            WriteCommand(Command.Nop());
                        }
                        catch (Exception ex)
                        {
                            if (_closeFlag != 1)
                            {
                                log(LogLevel.Error, string.Format("IO error - {0}", ex));
                                _delegate.OnIOError(this, ex);
                            }
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
                                log(LogLevel.Error, string.Format("IO error - {0}", ex));
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
                            string errMsg = Encoding.UTF8.GetString(data);
                            log(LogLevel.Error, string.Format("protocol error - {0}", errMsg));
                            _delegate.OnError(this, data);
                            break;
                        default:
                            // TODO: what would 'err' be in this case?
                            // https://github.com/bitly/go-nsq/blob/v1.0.3/conn.go#L518
                            var unknownFrameTypeEx = new Exception(string.Format("unknown frame type {0}", frameType));
                            log(LogLevel.Error, string.Format("IO error - {0}", unknownFrameTypeEx.Message));
                            _delegate.OnIOError(this, unknownFrameTypeEx);
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
                    log(LogLevel.Warning, string.Format("delaying close, {0} outstanding messages", messagesInFlight));
                }
                _wg.Done();
                log(LogLevel.Info, "readLoop exiting");
            }
        }

        private void writeLoop()
        {
            bool doLoop = true;
            using (var select =
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
                            log(LogLevel.Error, string.Format("error sending command {0} - {1}", cmd, ex.Message));
                            close();
                            // TODO: Create PR to remove unnecessary continue in go-nsq
                            // https://github.com/bitly/go-nsq/blob/v1.0.3/conn.go#L552
                        }
                    })
                    .CaseReceive(_msgResponseChan, resp =>
                    {
                        // Decrement this here so it is correct even if we can't respond to nsqd
                        var msgsInFlight = Interlocked.Decrement(ref _messagesInFlight);

                        if (resp.success)
                        {
                            log(LogLevel.Debug, string.Format("FIN {0}", resp.msg.IdHexString));
                            _delegate.OnMessageFinished(this, resp.msg);
                            if (resp.backoff)
                            {
                                _delegate.OnResume(this);
                            }
                        }
                        else
                        {
                            log(LogLevel.Debug, string.Format("REQ {0}", resp.msg.IdHexString));
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
                            log(LogLevel.Error, string.Format("error sending command {0} - {1}", resp.cmd, ex));
                            close();
                        }
                    })
                    .NoDefault(defer: true))
            {
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                while (doLoop)
                {
                    select.Execute();
                }
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
                        log(LogLevel.Warning, string.Format("draining... waiting for {0} messages in flight", msgsInFlight));
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

            _msgResponseChan.Send(new msgResponse
            {
                msg = m,
                cmd = Command.Requeue(m.ID, delay.Value),
                success = false,
                backoff = backoff
            });
        }

        internal void onMessageTouch(Message m)
        {
            Select
                .DebugName("Conn:onMessageTouch")
                .CaseSend("_cmdChan", _cmdChan, Command.Touch(m.ID), () => { })
                .CaseReceive("_exitChan", _exitChan, o => { })
                .NoDefault();
        }

        private void log(LogLevel lvl, string line)
        {
            // TODO: thread safety

            if (_logger == null)
                return;

            // TODO: Review format string
            _logger.Output(lvl, string.Format("{0} {1}",
                string.Format(_logFmt, ToString()), line));
        }
    }

    /// <summary>
    /// Identify request.
    /// </summary>
    [DataContract]
    public class IdentifyRequest
    {
        /// <summary>client_id</summary>
        [DataMember(Name = "client_id")]
        public string client_id { get; set; }
        /// <summary>hostname</summary>
        [DataMember(Name = "hostname")]
        public string hostname { get; set; }
        /// <summary>user_agent</summary>
        [DataMember(Name = "user_agent")]
        public string user_agent { get; set; }
        /// <summary>short_id (deprecated)</summary>
        [DataMember(Name = "short_id")]
        public string short_id { get; set; }
        /// <summary>long_id (deprecated)</summary>
        [DataMember(Name = "long_id")]
        public string long_id { get; set; }
        /// <summary>tls_v1</summary>
        [DataMember(Name = "tls_v1")]
        public bool tls_v1 { get; set; }
        /// <summary>deflate</summary>
        [DataMember(Name = "deflate")]
        public bool deflate { get; set; }
        /// <summary>deflate_level</summary>
        [DataMember(Name = "deflate_level")]
        public int deflate_level { get; set; }
        /// <summary>snappy</summary>
        [DataMember(Name = "snappy")]
        public bool snappy { get; set; }
        /// <summary>feature_negotiation</summary>
        [DataMember(Name = "feature_negotiation")]
        public bool feature_negotiation { get; set; }
        /// <summary>heartbeat_interval</summary>
        [DataMember(Name = "heartbeat_interval")]
        public int heartbeat_interval { get; set; }
        /// <summary>sample_rate</summary>
        [DataMember(Name = "sample_rate")]
        public int sample_rate { get; set; }
        /// <summary>output_buffer_size</summary>
        [DataMember(Name = "output_buffer_size")]
        public long output_buffer_size { get; set; }
        /// <summary>output_buffer_timeout</summary>
        [DataMember(Name = "output_buffer_timeout")]
        public int output_buffer_timeout { get; set; }
        /// <summary>msg_timeout</summary>
        [DataMember(Name = "msg_timeout")]
        public int msg_timeout { get; set; }
    }
}
