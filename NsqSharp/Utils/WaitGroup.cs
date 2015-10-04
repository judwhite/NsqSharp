using System;
using System.Threading;

namespace NsqSharp.Utils
{
    /// <summary>
    /// A WaitGroup waits for a collection of routines to finish. The main routine calls Add to set the number of routines to
    /// wait for. Then each of the routines runs and calls Done when finished. At the same time, Wait can be used to block until
    /// all routines have finished. See: http://golang.org/pkg/sync/#WaitGroup
    /// 
    /// NOTE: This is not as robust as Go's version. Go supports reusing a WaitGroup; here we're assuming Wait will only be
    /// called once. This may change in future implementations.
    /// </summary>
    public class WaitGroup
    {
        private readonly ManualResetEvent _wait = new ManualResetEvent(initialState: true);
        private readonly object _doneLocker = new object();
        private int _count;
        private bool _done = true;

        /// <summary>
        /// Add adds delta, which may be negative, to the WaitGroup counter. If the counter becomes zero, all goroutines blocked
        /// on Wait are released. If the counter goes negative, Add panics.
        ///
        /// Note that calls with a positive delta that occur when the counter is zero must happen before a Wait. Calls with a
        /// negative delta, or calls with a positive delta that start when the counter is greater than zero, may happen at any
        /// time. Typically this means the calls to Add should execute before the statement creating the routine or other event
        /// to be waited for.
        /// </summary>
        /// <param name="delta"></param>
        public void Add(int delta)
        {
            lock (_doneLocker)
            {
                if (_done)
                {
                    _done = false;
                    _wait.Reset();
                }
            }

            int num = Interlocked.Add(ref _count, delta);

            if (num <= 0)
            {
                lock (_doneLocker)
                {
                    _done = true;
                    _wait.Set();
                }

                if (num < 0)
                    throw new Exception("sync: negative WaitGroup counter");
            }
        }

        /// <summary>
        /// Done decrements the WaitGroup counter.
        /// </summary>
        public void Done()
        {
            Add(-1);
        }

        /// <summary>
        /// Wait blocks until the WaitGroup counter is zero.
        /// </summary>
        public void Wait()
        {
            _wait.WaitOne();
        }
    }
}
