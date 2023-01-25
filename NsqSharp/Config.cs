using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Attributes;
using NsqSharp.Utils.Extensions;

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
    /// Read only configuration values related to backoff. See <see cref="IBackoffStrategy"/>.
    /// </summary>
    public interface IBackoffConfig
    {
        /// <summary>Unit of time for calculating consumer backoff.</summary>
        TimeSpan BackoffMultiplier { get; }
        /// <summary>
        ///     The max backoff duration used for calculating whether the backoff level should increase.
        ///     See <see cref="IBackoffStrategy.Calculate"/>.
        /// </summary>
        TimeSpan MaxBackoffDuration { get; }
    }

    /// <summary>
    /// <see cref="IBackoffStrategy" /> defines a strategy for calculating the duration of time
    /// a consumer should backoff for a given attempt. See <see cref="ExponentialStrategy"/>
    /// and <see cref="FullJitterStrategy"/>.
    /// </summary>
    public interface IBackoffStrategy
    {
        /// <summary>Calculates the backoff time.</summary>
        /// <param name="backoffConfig">Read only configuration values related to backoff.</param>
        /// <param name="backoffLevel">
        ///     The backoff level (>= 1) used to calculate backoff duration.
        ///     <paramref name="backoffLevel"/> increases/decreases with successive failures/successes.
        /// </param>
        /// <returns>A <see cref="BackoffCalculation"/> object with the backoff duration and whether to increase
        ///          the backoff level.</returns>
        BackoffCalculation Calculate(IBackoffConfig backoffConfig, int backoffLevel);
    }

    /// <summary>
    /// <see cref="BackoffCalculation"/> is the return value from <see cref="IBackoffStrategy.Calculate"/>.
    /// </summary>
    public class BackoffCalculation
    {
        /// <summary>The backoff duration.</summary>
        public TimeSpan Duration { get; set; }
        /// <summary>Indicates whether the caller should increase the backoff level.</summary>
        public bool IncreaseBackoffLevel { get; set; }
    }

    /// <summary>
    /// <see cref="ExponentialStrategy"/> implements an exponential backoff strategy (default).
    /// </summary>
    public class ExponentialStrategy : IBackoffStrategy
    {
        /// <summary>
        /// Calculate returns a duration of time: 2^(backoffLevel-1) * <see cref="IBackoffConfig.BackoffMultiplier"/>.
        /// </summary>
        /// <param name="backoffConfig">Read only configuration values related to backoff.</param>
        /// <param name="backoffLevel">
        ///     The backoff level (>= 1) used to calculate backoff duration.
        ///     <paramref name="backoffLevel"/> increases/decreases with successive failures/successes.
        /// </param>
        /// <returns>A <see cref="BackoffCalculation"/> object with the backoff duration and whether to increase
        ///          the backoff level.</returns>
        public BackoffCalculation Calculate(IBackoffConfig backoffConfig, int backoffLevel)
        {
            var backoffDuration = new TimeSpan(backoffConfig.BackoffMultiplier.Ticks *
                (long)Math.Pow(2, backoffLevel - 1));

            return new BackoffCalculation
            {
                Duration = backoffDuration,
                IncreaseBackoffLevel = backoffDuration < backoffConfig.MaxBackoffDuration
            };
        }
    }

    /// <summary>
    /// FullJitterStrategy returns a random duration of time in the
    /// range [0, 2^(backoffLevel-1) * <see cref="IBackoffConfig.BackoffMultiplier"/>).
    /// Implements http://www.awsarchitectureblog.com/2015/03/backoff.html.
    /// </summary>
    public class FullJitterStrategy : IBackoffStrategy
    {
        private readonly Once rngOnce = new Once();
        private RNGCryptoServiceProvider rng;

        /// <summary>
        /// Calculate returns a random duration of time in the
        /// range [0, 2^(backoffLevel-1) * <see cref="IBackoffConfig.BackoffMultiplier"/>).
        /// </summary>
        /// <param name="backoffConfig">Read only configuration values related to backoff.</param>
        /// <param name="backoffLevel">
        ///     The backoff level (>= 1) used to calculate backoff duration.
        ///     <paramref name="backoffLevel"/> increases/decreases with successive failures/successes.
        /// </param>
        /// <returns>A <see cref="BackoffCalculation"/> object with the backoff duration and whether to increase
        ///          the backoff level.</returns>
        public BackoffCalculation Calculate(IBackoffConfig backoffConfig, int backoffLevel)
        {
            rngOnce.Do(() =>
                {
                    // lazily initialize the RNG
                    if (rng != null)
                        return;
                    rng = new RNGCryptoServiceProvider();
                }
            );

            var backoffDuration = new TimeSpan(backoffConfig.BackoffMultiplier.Ticks *
                (long)Math.Pow(2, backoffLevel - 1));

            int maxBackoffMilliseconds = (int)backoffDuration.TotalMilliseconds;
            int backoffMilliseconds = maxBackoffMilliseconds == 0 ? 0 : rng.Intn(maxBackoffMilliseconds);

            return new BackoffCalculation
            {
                Duration = TimeSpan.FromMilliseconds(backoffMilliseconds),
                IncreaseBackoffLevel = backoffDuration < backoffConfig.MaxBackoffDuration
            };
        }
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
    public class Config : IPleaseWorkConfig, IBackoffConfig
    {
        // used to Initialize, Validate
        private readonly List<configHandler> configHandlers;

        /// <summary>Deadline for establishing TCP connections.
        /// Default: 1s</summary>
        [Opt("dial_timeout"), Default("1s")]
        public TimeSpan DialTimeout { get; set; }

        /// <summary>Deadline for network reads.
        /// Range: 100ms-5m Default: 60s</summary>
        [Opt("read_timeout"), Min("100ms"), Max("5m"), Default("60s")]
        public TimeSpan ReadTimeout { get; set; }

        /// <summary>Deadline for network writes.
        /// Range: 100ms-5m Default: 10s</summary>
        [Opt("write_timeout"), Min("100ms"), Max("5m"), Default("10s")]
        public TimeSpan WriteTimeout { get; set; }

        /// <summary>Duration between polling lookupd for new producers.
        /// NOTE: when not using nsqlookupd, LookupdPollInterval represents the duration of time between
        /// reconnection attempts.
        /// Range: 10ms-5m Default: 60s</summary>
        [Opt("lookupd_poll_interval"), Min("10ms"), Max("5m"), Default("60s")]
        public TimeSpan LookupdPollInterval { get; set; }

        /// <summary>Fractional jitter to add to the lookupd pool loop. This helps evenly
        /// distribute requests even if multiple consumers restart at the same time.
        /// Range: 0-1 Default: 0.3
        /// </summary>
        [Opt("lookupd_poll_jitter"), Min(0), Max(1), Default(0.3)]
        public double LookupdPollJitter { get; set; }

        /// <summary>Maximum duration when REQueueing (for doubling of deferred requeue).
        /// Does not limit <see cref="TimeSpan"/> of manual requeue delays.
        /// Range: 0-60m Default: 15m</summary>
        [Opt("max_requeue_delay"), Min("0"), Max("60m"), Default("15m")]
        public TimeSpan MaxRequeueDelay { get; set; }

        /// <summary>Default requeue delay.
        /// Requeue calculation: <see cref="DefaultRequeueDelay"/> * <see cref="Message.Attempts"/>.
        /// Range: 0-60m Default: 90s</summary>
        [Opt("default_requeue_delay"), Min("0"), Max("60m"), Default("90s")]
        public TimeSpan DefaultRequeueDelay { get; set; }

        /// <summary>
        /// Backoff strategy, defaults to <see cref="ExponentialStrategy"/>. Overwrite this to define alternative backoff
        /// algorithms. See also <see cref="FullJitterStrategy"/>. Supported opt values: 'exponential', 'full_jitter'.
        /// </summary>
        [Opt("backoff_strategy"), Default("exponential")]
        public IBackoffStrategy BackoffStrategy { get; set; }
        /// <summary>Maximum amount of time to backoff when processing fails.
        /// Range: 0-60m Default: 2m</summary>
        [Opt("max_backoff_duration"), Min("0"), Max("60m"), Default("2m")]
        public TimeSpan MaxBackoffDuration { get; set; }
        /// <summary>Unit of time for calculating consumer backoff.
        /// Default backoff calculation: 2^(backoffLevel-1) * <see cref="BackoffMultiplier"/>.
        /// Will not exceed <see cref="MaxBackoffDuration"/>.
        /// See: <see cref="BackoffStrategy"/>
        /// Range: 1ms-60m Default: 1s</summary>
        [Opt("backoff_multiplier"), Min("1ms"), Max("60m"), Default("1s")]
        public TimeSpan BackoffMultiplier { get; set; }

        /// <summary>Maximum number of times this consumer will attempt to process a message before giving up.
        /// Range: 0-65535 Default: 5</summary>
        [Opt("max_attempts"), Min(0), Max(65535), Default(5)]
        public ushort MaxAttempts { get; set; }

        /// <summary>Duration to wait for a message from a producer when in a state where RDY
        /// counts are re-distributed (ie. max_in_flight &lt; num_producers).
        /// Range: 1s-5m Default: 10s</summary>
        [Opt("low_rdy_idle_timeout"), Min("1s"), Max("5m"), Default("10s")]
        public TimeSpan LowRdyIdleTimeout { get; set; }

        /// <summary>
        /// Duration between redistributing max-in-flight to connections.
        /// Range: 1ms-5s Default: 5s
        /// </summary>
        [Opt("rdy_redistribute_interval"), Min("1ms"), Max("5s"), Default("5s")]
        public TimeSpan RDYRedistributeInterval { get; set; }

        /// <summary>
        /// Redistribute RDY counts to active nsqd instances when others are idle.
        /// Default: <c>false</c>
        /// </summary>
        [Opt("rdy_redistribute_on_idle"), Default(false)]
        public bool RDYRedistributeOnIdle { get; set; }

        /// <summary>ClientID identifier sent to nsqd representing this client.
        /// Default: short hostname.</summary>
        [Opt("client_id")]
        public string ClientID { get; set; }

        /// <summary>Hostname identifier sent to nsqd representing this client.</summary>
        [Opt("hostname")]
        public string Hostname { get; set; }

        /// <summary>UserAgent identifier sent to nsqd representing this client, in the spirit of HTTP
        /// Default: NsqSharp/[version].</summary>
        [Opt("user_agent")]
        public string UserAgent { get; set; }

        /// <summary>Duration of time between heartbeats. This must be less than <see cref="ReadTimeout"/>.
        /// Default: 30s</summary>
        [Opt("heartbeat_interval"), Default("30s")]
        public TimeSpan HeartbeatInterval { get; set; }

        /// <summary>Receive a percentage of messages to sample the channel (requires nsqd 0.2.25+).
        /// See: https://github.com/nsqio/nsq/pull/223 for discussion.
        /// Range: 0-99 Default: 0 (disabled, receive all messages)
        /// </summary>
        [Opt("sample_rate"), Min(0), Max(99)]
        public int SampleRate { get; set; }

        // To set TLS config, use the following options:
        //
        // tls_min_version - String indicating the minimum version of tls acceptable ('ssl3.0', 'tls1.0', 'tls1.1', 'tls1.2')
        // tls_check_certificate_revocation - Bool indicating whether this client should check revocation
        // tls_insecure_skip_verify - Bool indicating whether this client should verify server certificates

        /// <summary>TLS configuration. Required to use TLS.</summary>
        [Opt("tls_config")]
        public TlsConfig TlsConfig { get; set; }

        // Compression Settings

        // TODO: Deflate
        /// <summary>Use Deflate compression</summary>
        [Opt("deflate")]
        private bool Deflate { get; set; }

        /// <summary>Deflate compression level (1-9, default: 6)</summary>
        [Opt("deflate_level"), Min(1), Max(9), Default(6)]
        private int DeflateLevel { get; set; }

        // TODO: Snappy
        /// <summary>Use Snappy compression</summary>
        [Opt("snappy")]
        private bool Snappy { get; set; }

        /// <summary>Size of the buffer (in bytes) used by nsqd for buffering writes to this connection.
        /// Default: 16384</summary>
        [Opt("output_buffer_size"), Default(16384)]
        public long OutputBufferSize { get; set; }
        /// <summary>
        /// <para>Timeout used by nsqd before flushing buffered writes (set to 0 to disable). Default: 250ms</para>
        ///
        /// <para>WARNING: configuring clients with an extremely low
        /// (&lt; 25ms) output_buffer_timeout has a significant effect
        /// on nsqd CPU usage (particularly with > 50 clients connected).</para>
        /// </summary>
        [Opt("output_buffer_timeout"), Default("250ms")]
        public TimeSpan OutputBufferTimeout { get; set; }

        /// <summary>Maximum number of messages to allow in flight (concurrency knob).
        /// Min: 0 Default: 1
        /// </summary>
        [Opt("max_in_flight"), Min(0), Default(1)]
        public int MaxInFlight { get; set; }

        /// <summary>The duration the server waits before auto-requeing a message sent to this client.
        /// nsqd will refuse a Consumer connection if this value exceeds the nsqd option set for "-max-msg-timeout" 
        /// (server "-max-msg-timeout" default = 15m).
        /// Default = Use server settings (server "-msg-timeout" default = 60s).
        /// </summary>
        [Opt("msg_timeout"), Min(0)]
        public TimeSpan MessageTimeout { get; set; }

        /// <summary>Secret for nsqd authentication (requires nsqd 0.2.29+).
        /// See: https://github.com/nsqio/nsq/pull/356, https://github.com/jehiah/nsqauth-contrib.
        /// </summary>
        [Opt("auth_secret")]
        public string AuthSecret { get; set; }
        bool IPleaseWorkConfig.Deflate { get => true; set => throw new NotImplementedException(); }
        int IPleaseWorkConfig.DeflateLevel { get => 1; set => throw new NotImplementedException(); }
        bool IPleaseWorkConfig.Snappy { get => true; set => throw new NotImplementedException(); }

        /// <summary>
        /// Initializes a new instance of Config.
        /// </summary>
        public Config()
        {
            configHandlers = new List<configHandler> { new structTagsConfig(), new tlsConfig() };
            setDefaults();
        }

        /// <summary>
        ///     <para>Takes an <paramref name="option"/> as a string and a <paramref name="value"/> and attempts to set the
        ///     appropriate configuration option.</para>
        ///     
        ///     <para>It attempts to coerce the <paramref name="value"/> into the right format depending on the named option
        ///     and the underlying type of the value passed in.</para>
        ///     
        ///     <para>Calls to <see cref="Set"/> that take a <see cref="TimeSpan"/> as an argument can be input as:
        ///     <list type="bullet">
        ///         <item><description>"1000ms" (a string parsed by <see cref="Time.ParseDuration"/>)</description></item>
        ///         <item><description>1000 (an integer interpreted as milliseconds)</description></item>
        ///         <item><description><see cref="TimeSpan.FromMilliseconds"/>(1000) (a literal <see cref="TimeSpan"/>
        ///         value)</description></item>
        ///     </list>
        ///     </para>
        ///     
        ///     <para>Calls to <see cref="Set"/> that take bool can be input as:
        ///     <list type="bullet">
        ///         <item><description>"true" (a string parsed by <see cref="bool.Parse"/>)</description></item>
        ///         <item><description>true (a boolean)</description></item>
        ///         <item><description>1 (an int where 1 == true and 0 == false)</description></item>
        ///     </list>
        ///     </para>
        /// </summary>
        /// <exception cref="Exception"><para>Thrown when an invalid <paramref name="option"/> name is specified.</para>
        ///     <para>Thrown when an invalid <paramref name="value"/> is specified for the given
        ///     <paramref name="option"/>.</para>
        /// </exception>
        /// <param name="option">The option name. See the <see cref="OptAttribute.Name"/> on <see cref="Config"/> properties
        ///     for valid option names.
        /// </param>
        /// <param name="value">The value.</param>
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

        /// <summary>Checks that all config values are within specified min/max ranges.</summary>
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

                // TODO: PR go-nsq: this check was removed, seems still valid since the value can be set through code
                // https://github.com/nsqio/go-nsq/commit/dd8e5fc4ad80922d884ece51f5574af3fa4f14d3#diff-b4bda758a2aef091432646c354b4dc59L376
                if (c.BackoffStrategy == null)
                    throw new Exception(string.Format("BackoffStrategy cannot be null"));
            }
        }

        internal class tlsConfig : configHandler
        {
            public bool HandlesOption(Config c, string option)
            {
                switch (option)
                {
                    case "tls_min_version":
                    case "tls_check_certificate_revocation":
                    case "tls_insecure_skip_verify":
                        return true;
                }
                return false;
            }

            public void Set(Config c, string option, object value)
            {
                var tlsConfig = c.TlsConfig != null
                                    ? c.TlsConfig.Clone()
                                    : new TlsConfig();

                switch (option)
                {
                    case "tls_min_version":
                        var version = (string)value;
                        switch (version)
                        {
                            case "ssl3.0":
                                tlsConfig.MinVersion = SslProtocols.Ssl3;
                                break;
                            case "tls1.0":
                                tlsConfig.MinVersion = SslProtocols.Tls;
                                break;
#if !NETFX_4_0
                            case "tls1.1":
                                tlsConfig.MinVersion = SslProtocols.Tls11;
                                break;
                            case "tls1.2":
                                tlsConfig.MinVersion = SslProtocols.Tls12;
                                break;
#endif
                            default:
                                throw new Exception(string.Format("ERROR: {0} is not a tls version", value));
                        }
                        break;
                    case "tls_check_certificate_revocation":
                        bool checkCertificationRevocation = value.Coerce<bool>();
                        tlsConfig.CheckCertificateRevocation = checkCertificationRevocation;
                        break;
                    case "tls_insecure_skip_verify":
                        bool insecureSkipVerify = value.Coerce<bool>();
                        tlsConfig.InsecureSkipVerify = insecureSkipVerify;
                        break;
                    default:
                        throw new Exception(string.Format("unknown option {0}", option));
                }

                c.TlsConfig = tlsConfig;
            }

            public void Validate(Config c)
            {
                // no op
            }
        }

        /// <summary>Clones (makes a copy) of this instance.</summary>
        /// <returns>A copy of this object.</returns>
        public Config Clone()
        {
            var newConfig = new Config();

            newConfig.BackoffStrategy = BackoffStrategy;

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
