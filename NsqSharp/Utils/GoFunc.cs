using System;
using System.Threading;
using NsqSharp.Core;
using NsqSharp.Utils.Loggers;

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
        /// <param name="threadName">The name to assign to the thread (optional).</param>
        public static void Run(Action action, string threadName = null)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var t = new Thread(() =>
                               {
                                   try
                                   {
                                       action();
                                   }
                                   catch (ThreadAbortException)
                                   {
                                   }
                                   catch (Exception ex)
                                   {
                                       var logger = new TraceLogger();
                                       logger.Output(LogLevel.Critical, string.Format("{0} - {1}", threadName, ex));
                                       throw;
                                   }
                               }
                );

            if (threadName != null)
                t.Name = threadName;

            t.IsBackground = true;
            t.Start();
        }
    }
}
