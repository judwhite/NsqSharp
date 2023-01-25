using NsqSharp.Utils;

namespace NsqSharp
{
    public interface IPleaseWorkConfig
    {

        public void Validate();

        public Config Clone();
        public TimeSpan DialTimeout { get; set; }

        public TimeSpan ReadTimeout { get; set; }
        public TimeSpan WriteTimeout { get; set; }
        public TimeSpan LookupdPollInterval { get; set; }

        public double LookupdPollJitter { get; set; }

        public TimeSpan MaxRequeueDelay { get; set; }
        public TimeSpan DefaultRequeueDelay { get; set; }


        public IBackoffStrategy BackoffStrategy { get; set; }

        public TimeSpan MaxBackoffDuration { get; set; }
        public TimeSpan BackoffMultiplier { get; set; }
        public ushort MaxAttempts { get; set; }
        public TimeSpan LowRdyIdleTimeout { get; set; }

        public TimeSpan RDYRedistributeInterval { get; set; }

        public bool RDYRedistributeOnIdle { get; set; }

  
        public string ClientID { get; set; }


        public string Hostname { get; set; }

 
        public string UserAgent { get; set; }


        public TimeSpan HeartbeatInterval { get; set; }

        public int SampleRate { get; set; }

        public TlsConfig TlsConfig { get; set; }


        public bool Deflate { get; set; }

    
        public int DeflateLevel { get; set; }

        public bool Snappy { get; set; }


        public long OutputBufferSize { get; set; }

        public TimeSpan OutputBufferTimeout { get; set; }


        public int MaxInFlight { get; set; }

  
        public TimeSpan MessageTimeout { get; set; }


        public string AuthSecret { get; set; }
    }
}