using System;

namespace NsqSharp.Utils.Extensions
{
    /// <summary>
    /// <see cref="Object"/> extension methods.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Coerce a value to the specified type <typeparamref name="T"/>. Supports Duration and Bool string/int formats.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="value">The value to coerce.</param>
        /// <returns>The value as type <typeparamref name="T"/>.</returns>
        public static T Coerce<T>(this object value)
        {
            return (T)Coerce(value, typeof(T));
        }

        /// <summary>
        /// Coerce a value to the specified <paramref name="targetType"/>. Supports Duration and Bool string/int formats.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The value as the <paramref name="targetType"/>.</returns>
        public static object Coerce(this object value, Type targetType)
        {
            if (value == null)
                return null;

            var valueType = value.GetType();
            if (valueType == targetType)
                return value;

            if (targetType == typeof(ushort))
            {
                if (valueType == typeof(int))
                    return Convert.ToUInt16(value);
                if (valueType == typeof(string))
                    return ushort.Parse((string)value);
            }
            else if (targetType == typeof(int))
            {
                if (valueType == typeof(string))
                    return int.Parse((string)value);
            }
            else if (targetType == typeof(long))
            {
                if (valueType == typeof(string))
                    return long.Parse((string)value);
                if (valueType == typeof(int))
                    return Convert.ToInt64(value);
            }
            else if (targetType == typeof(double))
            {
                if (valueType == typeof(string))
                    return double.Parse((string)value);
                if (valueType == typeof(int))
                    return Convert.ToDouble(value);
            }
            else if (targetType == typeof(bool))
            {
                if (valueType == typeof(string))
                {
                    string strValue = (string)value;
                    if (strValue == "0")
                        return false;
                    else if (strValue == "1")
                        return true;
                    return bool.Parse(strValue);
                }
                if (valueType == typeof(int))
                {
                    int intValue = (int)value;
                    if (intValue == 0)
                        return false;
                    else if (intValue == 1)
                        return true;
                }
            }
            else if (targetType == typeof(TimeSpan))
            {
                if (valueType == typeof(string))
                {
                    string strValue = (string)value;

                    long ms;
                    if (long.TryParse(strValue, out ms))
                        return TimeSpan.FromMilliseconds(ms);

                    long ns = Time.ParseDuration(strValue);
                    return new TimeSpan(ns / 100);
                }
                if (valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(ulong))
                {
                    long ms = Convert.ToInt64(value);
                    return TimeSpan.FromMilliseconds(ms);
                }
            }
            else if (targetType == typeof(IBackoffStrategy))
            {
                if (valueType == typeof(string))
                {
                    string strValue = (string)value;
                    switch (strValue)
                    {
                        case "":
                        case "exponential":
                            return new ExponentialStrategy();
                        case "full_jitter":
                            return new FullJitterStrategy();
                    }
                }
                else if (value is IBackoffStrategy)
                {
                    return value;
                }
            }

            throw new Exception(string.Format("failed to coerce ({0} {1}) to {2}", value, valueType, targetType));
        }
    }
}
