using System;
using System.Threading;

namespace NsqSharp.Utils
{
    /// <summary>
    /// Go routines
    /// </summary>
    public static class GoFunc
    {
        /// <summary>
        /// Run a new "goroutine".
        /// </summary>
        /// <param name="action">The method to execute.</param>
        public static void Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var t = new Thread(() => action());
            t.IsBackground = true;
            t.Start();
        }
    }
}
