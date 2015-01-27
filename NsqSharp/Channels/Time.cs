using System;
using System.Threading.Tasks;

namespace NsqSharp.Channels
{
    internal static class Time
    {
        /// <summary>
        /// Creates a channel which fires after the specified timeout.
        /// </summary>
        public static Chan<bool> After(TimeSpan timeout)
        {
            var fireAt = DateTime.UtcNow + timeout;

            var timeoutChan = new Chan<bool>();

            Task.Factory.StartNew(() =>
                                  {
                                      var sleep = (fireAt - DateTime.UtcNow);
                                      if (sleep > TimeSpan.Zero)
                                      {
                                          Task.Delay(sleep).Wait();
                                      }

                                      timeoutChan.Send(default(bool));
                                  });

            return timeoutChan;
        }
    }
}
