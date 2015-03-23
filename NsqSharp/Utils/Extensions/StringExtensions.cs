namespace NsqSharp.Utils.Extensions
{
    /// <summary>
    /// <see cref="System.String"/> extension methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Indicates whether a specified string is <c>null</c>, empty, or consists only of white-space characters.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string value)
        {
#if !NETFX_3_5
            return string.IsNullOrWhiteSpace(value);
#else
            if (value == null)
                return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
#endif
        }
    }
}
