using NsqSharp;
using NsqSharp.Utils;

internal class BackOffConfigClass : NsqSharp.IPleaseWorkConfig
{
    public TimeSpan BackoffMultiplier => throw new NotImplementedException();

    public TimeSpan MaxBackoffDuration => throw new NotImplementedException();

    public TimeSpan DialTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan ReadTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan WriteTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan LookupdPollInterval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public double LookupdPollJitter { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan MaxRequeueDelay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan DefaultRequeueDelay { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public IBackoffStrategy BackoffStrategy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ushort MaxAttempts { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan LowRdyIdleTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan RDYRedistributeInterval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool RDYRedistributeOnIdle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string ClientID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string Hostname { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string UserAgent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan HeartbeatInterval { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int SampleRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TlsConfig TlsConfig { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool Deflate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int DeflateLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool Snappy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public long OutputBufferSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan OutputBufferTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int MaxInFlight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public TimeSpan MessageTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string AuthSecret { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    TimeSpan IPleaseWorkConfig.MaxBackoffDuration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    TimeSpan IPleaseWorkConfig.BackoffMultiplier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public Config Clone()
    {
       return new Config();
    }

    public void Validate()
    {
        
    }
}