using System;

namespace NsqSharp.Utils.Extensions
{
    /// <summary>
    /// <see cref="DateTime"/> extension methods.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// UnixNano returns t as a Unix time, the number of nanoseconds elapsed since January 1, 1970 UTC. The result is
        /// undefined if the Unix time in nanoseconds cannot be represented by an int64. Note that this means the result of
        /// calling UnixNano on the zero Time is undefined.
        /// </summary>
        public static long UnixNano(this DateTime dateTime)
        {
            return (dateTime - _epoch).Ticks * 100;
        }

        /// <summary>
        /// Unix returns t as a Unix time, the number of seconds elapsed since January 1, 1970 UTC.
        /// </summary>
        public static long Unix(this DateTime dateTime)
        {
            return (long)((dateTime - _epoch).TotalSeconds);
        }
    }
}
