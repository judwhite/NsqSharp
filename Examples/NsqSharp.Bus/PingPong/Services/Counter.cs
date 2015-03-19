using System;
using System.Diagnostics;
using System.Threading;

namespace PingPong.Services
{
    // used in this demo for counting handled messages
    public class Counter : ICounter
    {
        private readonly Stopwatch _stopwatch;
        private int _count;
        
        public Counter() { _stopwatch = Stopwatch.StartNew(); }
        
        public int Next() { return Interlocked.Increment(ref _count); }
        public int Current { get { return _count; } }
        public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }
    }
}
