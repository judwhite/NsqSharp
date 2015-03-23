using System;

namespace PingPong.Services
{
    public interface ICounter
    {
        int Next();
        int Current { get; }
        TimeSpan Elapsed { get; }
    }
}
