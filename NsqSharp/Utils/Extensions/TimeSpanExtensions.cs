using System;

namespace NsqSharp.Utils.Extensions
{
    /// <summary>
    /// <see cref="TimeSpan"/> extension methods.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Gets the <paramref name="timeSpan"/> as nanoseconds.
        /// </summary>
        public static long Nanoseconds(this TimeSpan timeSpan)
        {
            return timeSpan.Ticks*100;
        }
    }
}
