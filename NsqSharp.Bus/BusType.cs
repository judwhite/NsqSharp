namespace NsqSharp.Bus
{
    /// <summary>
    /// Bus type
    /// </summary>
    public enum BusType
    {
        /// <summary>Use NSQ.</summary>
        Nsq,
        /// <summary>Use an in memory bus for local development and unit testing.</summary>
        InMemory
    }
}
