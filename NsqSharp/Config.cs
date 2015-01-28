using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NsqSharp.Attributes;

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

    internal interface defaultsHandler
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
    /// Use Set(key string, value interface{}) as an alternate way to set parameters
    /// </summary>
    public class Config
    {
        //private bool initialized;

        // used to Initialize, Validate
        //private List<configHandler> configHandlers;

        /// <summary>Deadline for network reads</summary>
        [Opt("read_timeout"), Min("100ms"), Max("5m"), Default("60s", isDuration: true)]
        public TimeSpan ReadTimeout { get; internal set; }

        /// <summary>Deadline for network writes</summary>
        [Opt("read_timeout"), Min("100ms"), Max("5m"), Default("1s", isDuration: true)]
        public TimeSpan WriteTimeout { get; internal set; }

        /// <summary>Duration between polling lookupd for new producers</summary>
        [Opt("lookupd_poll_interval"), Min("5s"), Max("5m"), Default("60s", isDuration: true)]
        public TimeSpan LookupdPollInterval { get; private set; }
        /// <summary>Fractional jitter to add to the lookupd pool loop. This helps evenly
        /// distribute requests even if multiple consumers restart at the same time</summary>
        [Opt("lookupd_poll_jitter"), Min(0), Max(1), Default(0.3)]
        public double LookupdPollJitter { get; private set; }

        /// <summary>Maximum duration when REQueueing (for doubling of deferred requeue)</summary>
        [Opt("max_requeue_delay"), Min("0"), Max("60m"), Default("15m", isDuration: true)]
        public TimeSpan MaxRequeueDelay { get; private set; }
        /// <summary>Default requeue delay</summary>
        [Opt("default_requeue_delay"), Min("0"), Max("60m"), Default("90s", isDuration: true)]
        public TimeSpan DefaultRequeueDelay { get; private set; }
        /// <summary>Unit of time for calculating consumer backoff</summary>
        [Opt("backoff_multiplier"), Min("0"), Max("60m"), Default("1s", isDuration: true)]
        public TimeSpan BackoffMultiplier { get; private set; }

        /// <summary>Maximum number of times this consumer will attempt to process a message before giving up</summary>
        [Opt("max_attempts"), Min(0), Max(65535), Default(5)]
        public ushort MaxAttempts { get; private set; }
        /// <summary>Amount of time in seconds to wait for a message from a producer when in a state where RDY
        /// counts are re-distributed (ie. max_in_flight &lt; num_producers)</summary>
        [Opt("low_rdy_idle_timeout"), Min("1s"), Max("5m"), Default("10s", isDuration: true)]
        public TimeSpan LowRdyIdleTimeout { get; private set; }

        /// <summary>client_id identifier sent to nsqd representing this client (defaults: short hostname)</summary>
        [Opt("client_id")]
        public string ClientID { get; private set; }
        /// <summary>hostname identifier sent to nsqd representing this client</summary>
        [Opt("hostname")]
        public string Hostname { get; private set; }
        /// <summary>user_agent identifier sent to nsqd representing this client, in the spirit of HTTP
        /// (default: [client_library_name]/[version])</summary>
        [Opt("user_agent")]
        public string UserAgent { get; private set; }

        /// <summary>Duration of time between heartbeats. This must be less than ReadTimeout</summary>
        [Opt("heartbeat_interval"), Default("30s", isDuration: true)]
        public TimeSpan HeartbeatInterval { get; private set; }
        /// <summary>Integer percentage to sample the channel (requires nsqd 0.2.25+)</summary>
        [Opt("sample_rate"), Min(0), Max(99)]
        public int SampleRate { get; private set; }
    }
}
