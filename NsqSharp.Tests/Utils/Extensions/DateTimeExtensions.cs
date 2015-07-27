using System;

namespace NsqSharp.Tests.Utils.Extensions
{
    public static class DateTimeExtensions
    {
        public static string Formatted(this DateTime dateTime)
        {
            return string.Format("{0:HH:mm:ss.fff}", dateTime);
        }
    }
}
