using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using NsqSharp.Attributes;
using NsqSharp.Extensions;
using NsqSharp.Go;

namespace NsqSharp
{
    /// <summary>
    /// Define handlers for setting config defaults, and setting config values from command line arguments or config files
    /// </summary>
    internal interface configHandler
    {
        bool HandlesOption(Config c, string option);
        void Set(Config c, string option, object value);
        void Validate(Config c);
    }

    internal interface defaultsHandler : configHandler
    {
        void SetDefaults(Config c);
    }

    /// <summary>
    /// Config is a struct of NSQ options
    ///
    /// The only valid way to create a Config is via NewConfig, using a struct literal will panic.
    /// After Config is passed into a high-level type (like Consumer, Producer, etc.) the values are no
    /// longer mutable (they are copied).
    ///
    /// Use Set(string option, object value) as an alternate way to set parameters
    /// </summary>
    public class Config
    {
        // used to Initialize, Validate
        private readonly List<configHandler> configHandlers;

        /// <summary>Deadline for network reads</summary>
        [Opt("read_timeout"), Min("100ms"), Max("5m"), Default("60s")]
        public TimeSpan ReadTimeout { get; internal set; }

        /// <summary>Deadline for network writes</summary>
        [Opt("write_timeout"), Min("100ms"), Max("5m"), Default("1s")]
        public TimeSpan WriteTimeout { get; internal set; }

        /// <summary>Duration between polling lookupd for new producers</summary>
        [Opt("lookupd_poll_interval"), Min("5s"), Max("5m"), Default("60s")]
        public TimeSpan LookupdPollInterval { get; set; }

        /// <summary>Fractional jitter to add to the lookupd pool loop. This helps evenly
        /// distribute requests even if multiple consumers restart at the same time</summary>
        [Opt("lookupd_poll_jitter"), Min(0), Max(1), Default(0.3)]
        public double LookupdPollJitter { get; set; }

        /// <summary>Maximum duration when REQueueing (for doubling of deferred requeue)</summary>
        [Opt("max_requeue_delay"), Min("0"), Max("60m"), Default("15m")]
        public TimeSpan MaxRequeueDelay { get; set; }

        /// <summary>Default requeue delay</summary>
        [Opt("default_requeue_delay"), Min("0"), Max("60m"), Default("90s")]
        public TimeSpan DefaultRequeueDelay { get; set; }

        /// <summary>Unit of time for calculating consumer backoff</summary>
        [Opt("backoff_multiplier"), Min("0"), Max("60m"), Default("1s")]
        public TimeSpan BackoffMultiplier { get; set; }

        /// <summary>Maximum number of times this consumer will attempt to process a message before giving up</summary>
        [Opt("max_attempts"), Min(0), Max(65535), Default(5)]
        public ushort MaxAttempts { get; set; }

        /// <summary>Amount of time to wait for a message from a producer when in a state where RDY
        /// counts are re-distributed (ie. max_in_flight &lt; num_producers)</summary>
        [Opt("low_rdy_idle_timeout"), Min("1s"), Max("5m"), Default("10s")]
        public TimeSpan LowRdyIdleTimeout { get; set; }

        /// <summary>client_id identifier sent to nsqd representing this client (defaults: short hostname)</summary>
        [Opt("client_id")]
        public string ClientID { get; set; }

        /// <summary>hostname identifier sent to nsqd representing this client</summary>
        [Opt("hostname")]
        public string Hostname { get; set; }

        /// <summary>user_agent identifier sent to nsqd representing this client, in the spirit of HTTP
        /// (default: [client_library_name]/[version])</summary>
        [Opt("user_agent")]
        public string UserAgent { get; set; }

        /// <summary>Duration of time between heartbeats. This must be less than ReadTimeout</summary>
        [Opt("heartbeat_interval"), Default("30s")]
        public TimeSpan HeartbeatInterval { get; set; }

        /// <summary>Integer percentage to sample the channel (requires nsqd 0.2.25+)</summary>
        [Opt("sample_rate"), Min(0), Max(99)]
        public int SampleRate { get; set; }

        // To set TLS config, use the following options:
        //
        // tls_v1 - Bool enable TLS negotiation
        // tls_root_ca_file - String path to file containing root CA
        // tls_insecure_skip_verify - Bool indicates whether this client should verify server certificates
        // tls_cert - String path to file containing public key for certificate
        // tls_key - String path to file containing private key for certificate
        // tls_min_version - String indicating the minimum version of tls acceptable ('ssl3.0', 'tls1.0', 'tls1.1', 'tls1.2')

        /// <summary>Enable TLS negotiation</summary>
        [Opt("tls_v1")]
        public bool TlsV1 { get; set; }

        /// <summary>TLS configuration</summary>
        [Opt("tls_config")]
        public TlsConfig TlsConfig { get; set; }

        // Compression Settings

        /// <summary>Use Deflate compression</summary>
        [Opt("deflate")]
        public bool Deflate { get; set; }

        /// <summary>Deflate compression level (1-9, default: 6)</summary>
        [Opt("deflate_level"), Min(1), Max(9), Default(6)]
        public int DeflateLevel { get; set; }

        /// <summary>Use Snappy compression</summary>
        [Opt("snappy")]
        public bool Snappy { get; set; }

        /// <summary>Size of the buffer (in bytes) used by nsqd for buffering writes to this connection</summary>
        [Opt("output_buffer_size"), Default(16384)]
        public long OutputBufferSize { get; set; }
        /// <summary>
        /// Timeout used by nsqd before flushing buffered writes (set to 0 to disable).
        ///
        /// WARNING: configuring clients with an extremely low
        /// (&lt; 25ms) output_buffer_timeout has a significant effect
        /// on nsqd CPU usage (particularly with > 50 clients connected).
        /// </summary>
        [Opt("output_buffer_timeout"), Default("250ms")]
        public TimeSpan OutputBufferTimeout { get; set; }

        /// <summary>Maximum number of messages to allow in flight (concurrency knob)</summary>
        [Opt("max_in_flight"), Min(0), Default(1)]
        public int MaxInFlight { get; set; }

        /// <summary>Maximum amount of time to backoff when processing fails 0 == no backoff</summary>
        [Opt("max_backoff_duration"), Min("0"), Max("60m"), Default("2m")]
        public TimeSpan MaxBackoffDuration { get; set; }
        /// <summary>The server-side message timeout for messages delivered to this client</summary>
        [Opt("msg_timeout"), Min(0)]
        public TimeSpan MsgTimeout { get; set; }

        /// <summary>Secret for nsqd authentication (requires nsqd 0.2.29+)</summary>
        [Opt("auth_secret")]
        public string AuthSecret { get; set; }

        /// <summary>
        /// Initializes a new instance of Config.
        /// </summary>
        public Config()
        {
            configHandlers = new List<configHandler> { new structTagsConfig(), new tlsConfig() };
            setDefaults();
        }

        /// <summary>
        /// Set takes an option as a string and a value as an interface and
        /// attempts to set the appropriate configuration option.
        ///
        /// It attempts to coerce the value into the right format depending on the named
        /// option and the underlying type of the value passed in.
        ///
        /// Calls to Set() that take a time.Duration as an argument can be input as:
        ///
        ///     "1000ms" (a string parsed by time.ParseDuration())
        ///     1000 (an integer interpreted as milliseconds)
        ///     1000*time.Millisecond (a literal time.Duration value)
        ///
        /// Calls to Set() that take bool can be input as:
        ///
        ///     "true" (a string parsed by strconv.ParseBool())
        ///     true (a boolean)
        ///     1 (an int where 1 == true and 0 == false)
        ///
        /// It returns an error for an invalid option or value.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="value"></param>
        public void Set(string option, object value)
        {
            option = option.Replace("-", "_");
            foreach (var h in configHandlers)
            {
                if (h.HandlesOption(this, option))
                {
                    h.Set(this, option, value);
                    return;
                }
            }

            throw new Exception(string.Format("invalid option {0}", option));
        }

        /// <summary>
        /// Validate checks that all values are within specified min/max ranges
        /// </summary>
        public void Validate()
        {
            foreach (var h in configHandlers)
            {
                h.Validate(this);
            }
        }

        private void setDefaults()
        {
            foreach (var h in configHandlers)
            {
                var hh = h as defaultsHandler;
                if (hh != null)
                {
                    hh.SetDefaults(this);
                }
            }
        }

        internal class structTagsConfig : defaultsHandler
        {
            public bool HandlesOption(Config c, string option)
            {
                var typ = c.GetType();
                foreach (var field in typ.GetProperties())
                {
                    var opt = field.Get<OptAttribute>();
                    if (opt != null && opt.Name == option)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void Set(Config c, string option, object value)
            {
                var typ = c.GetType();
                foreach (var field in typ.GetProperties())
                {
                    var opt = field.Get<OptAttribute>();
                    if (opt == null || opt.Name != option)
                        continue;

                    var min = field.Get<MinAttribute>();
                    var max = field.Get<MaxAttribute>();

                    var coercedVal = opt.Coerce(value, field.PropertyType);

                    if (min != null)
                    {
                        var coercedMinVal = (IComparable)opt.Coerce(min.Value, field.PropertyType);
                        if (coercedMinVal.CompareTo(coercedVal) == 1)
                            throw new Exception(string.Format("invalid {0} ! {1} < {2}", opt.Name, coercedVal, coercedMinVal));
                    }

                    if (max != null)
                    {
                        var coercedMaxVal = (IComparable)opt.Coerce(max.Value, field.PropertyType);
                        if (coercedMaxVal.CompareTo(coercedVal) == -1)
                            throw new Exception(string.Format("invalid {0} ! {1} > {2}", opt.Name, coercedVal, coercedMaxVal));
                    }

                    field.SetValue(c, coercedVal, index: null);
                    return;
                }

                throw new Exception(string.Format("unknown option {0}", option));
            }

            public void SetDefaults(Config c)
            {
                Type typ = c.GetType();
                foreach (var field in typ.GetProperties())
                {
                    var opt = field.Get<OptAttribute>();
                    var defaultValue = field.Get<DefaultAttribute>();
                    if (opt == null || defaultValue == null)
                        continue;

                    c.Set(opt.Name, defaultValue.Value);
                }

                string hostname = OS.Hostname();

                c.ClientID = hostname.Split(new[] { '.' })[0];
                c.Hostname = hostname;
                c.UserAgent = string.Format("{0}/{1}", ClientInfo.ClientName, ClientInfo.Version);
            }

            public void Validate(Config c)
            {
                var typ = c.GetType();
                foreach (var field in typ.GetProperties())
                {
                    MinAttribute min = field.Get<MinAttribute>();
                    MaxAttribute max = field.Get<MaxAttribute>();

                    if (min == null && max == null)
                        continue;

                    object value = field.GetValue(c, index: null);

                    var opt = field.Get<OptAttribute>();
                    if (min != null)
                    {
                        var coercedMinVal = (IComparable)opt.Coerce(min.Value, field.PropertyType);
                        if (coercedMinVal.CompareTo(value) == 1)
                            throw new Exception(string.Format("invalid {0} ! {1} < {2}", opt.Name, value, coercedMinVal));
                    }
                    if (max != null)
                    {
                        var coercedMaxVal = (IComparable)opt.Coerce(max.Value, field.PropertyType);
                        if (coercedMaxVal.CompareTo(value) == -1)
                            throw new Exception(string.Format("invalid {0} ! {1} > {2}", opt.Name, value, coercedMaxVal));
                    }
                }

                if (c.HeartbeatInterval > c.ReadTimeout)
                {
                    throw new Exception(string.Format("HeartbeatInterval {0} must be less than ReadTimeout {1}",
                        c.HeartbeatInterval, c.ReadTimeout));
                }
            }
        }

        internal class tlsConfig : configHandler
        {
            private string certFile { get; set; }
            private string keyFile { get; set; }

            public bool HandlesOption(Config c, string option)
            {
                switch (option)
                {
                    case "tls_root_ca_file":
                    case "tls_insecure_skip_verify":
                    case "tls_cert":
                    case "tls_key":
                    case "tls_min_version":
                        return true;
                }
                return false;
            }

            public void Set(Config c, string option, object value)
            {
                if (c.TlsConfig == null)
                {
                    c.TlsConfig = new TlsConfig
                    {
                        MinVersion = SslProtocols.Tls,
#if NET40
                        MaxVersion = SslProtocols.Tls
#else
                        MaxVersion = SslProtocols.Tls12
#endif
                    };
                }

                switch (option)
                {
                    case "tls_cert":
                    case "tls_key":
                        // TODO: Test
                        if (option == "tls_cert")
                            certFile = (string)value;
                        else
                            keyFile = (string)value;

                        if (!string.IsNullOrEmpty(certFile) && !string.IsNullOrEmpty(keyFile) &&
                            c.TlsConfig.Certificates.Count == 0)
                        {
                            c.TlsConfig.Certificates.Import(certFile);
                            c.TlsConfig.Certificates.Import(keyFile);
                        }
                        return;

                    case "tls_root_ca_file":
                        // TODO: Test
                        string path = (string)value;
                        var certificates = PEM(File.ReadAllText(path));
                        c.TlsConfig.RootCAs = certificates;
                        return;

                    case "tls_insecure_skip_verify":
                        bool coercedVal = value.Coerce<bool>();
                        c.TlsConfig.InsecureSkipVerify = coercedVal;
                        return;

                    case "tls_min_version":
                        var version = (string)value;
                        switch (version)
                        {
                            case "ssl3.0":
                                c.TlsConfig.MinVersion = SslProtocols.Ssl3;
                                break;
                            case "tls1.0":
                                c.TlsConfig.MinVersion = SslProtocols.Tls;
                                break;
#if !NET40
                            case "tls1.1":
                                c.TlsConfig.MinVersion = SslProtocols.Tls11;
                                return;
                            case "tls1.2":
                                c.TlsConfig.MinVersion = SslProtocols.Tls12;
                                return;
#endif
                            default:
                                throw new Exception(string.Format("ERROR: {0} is not a tls version", value));
                        }
                        return;
                }

                throw new Exception(string.Format("unknown option {0}", option));
            }

            public void Validate(Config c)
            {
                // no op
            }

            private static X509Certificate2Collection PEM(string pem)
            {
                // TODO: Test

                const string beginCert = "-----BEGIN CERTIFICATE-----";
                const string endCert = "-----END CERTIFICATE-----";

                var certificates = new X509Certificate2Collection();

                int end;
                for (int start = pem.IndexOf(beginCert); start >= 0; start = pem.IndexOf(beginCert, end))
                {
                    start += beginCert.Length;

                    end = pem.IndexOf(endCert, start);
                    if (end == -1)
                        throw new Exception(string.Format("'{0}' not found after index {1}", endCert, start));

                    byte[] rawData = Convert.FromBase64String(pem.Substring(start, end - start));
                    certificates.Import(rawData);
                }

                if (certificates.Count == 0)
                {
                    throw new Exception("no certificates found");
                }

                return certificates;
            }
        }

        /// <summary>
        /// Clones (makes a copy) of this instance.
        /// </summary>
        public Config Clone()
        {
            var newConfig = new Config();

            var typ = GetType();
            foreach (var field in typ.GetProperties())
            {
                var opt = field.Get<OptAttribute>();
                if (opt != null)
                {
                    newConfig.Set(opt.Name, field.GetValue(this, index: null));
                }
            }

            return newConfig;
        }
    }
}
