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

        public Counter()
        {
            _stopwatch = new Stopwatch();
        }

        public int Next()
        {
            int num = Interlocked.Increment(ref _count);
            if (num == 1)
                _stopwatch.Start();
            return num;
        }

        public int Current { get { return _count; } }
        public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }
    }
}
