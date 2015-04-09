using System;

namespace NsqSharp.Utils
{
    // https://golang.org/src/sync/once.go

    /// <summary>
    /// Once is an object that will perform exactly one action.
    /// </summary>
    public class Once
    {
        private readonly object _mtx = new object();
        private bool _done;

        /// <summary>
        /// Do calls the function f if and only if Do is being called for the first time for this instance of Once.
        /// </summary>
        public void Do(Action f)
        {
            if (_done)
                return;

            lock (_mtx)
            {
                if (!_done)
                {
                    try
                    {
                        f();
                    }
                    finally
                    {
                        _done = true;
                    }
                }
            }
        }
    }
}
