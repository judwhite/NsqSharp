using System;
using NsqSharp.Go;

namespace NsqSharp.Attributes
{
    /// <summary>
    /// Min value attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MinAttribute : Attribute
    {
        private readonly object _value;

        /// <summary>
        /// Initializes a new instance of the MinAttribute class.
        /// </summary>
        /// <param name="value">The minimum value.</param>
        public MinAttribute(string value)
            : this((object)value, isDuration: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MinAttribute class.
        /// </summary>
        /// <param name="value">The minimum value.</param>
        public MinAttribute(ushort value)
            : this((object)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MinAttribute class.
        /// </summary>
        /// <param name="value">The minimum value.</param>
        public MinAttribute(int value)
            : this((object)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MinAttribute class.
        /// </summary>
        /// <param name="value">The minimum value.</param>
        public MinAttribute(double value)
            : this((object)value)
        {
        }

        private MinAttribute(object value, bool isDuration = false)
        {
            _value = value;

            if (isDuration)
            {
                // convert from nanoseconds to ticks (100-nanosecond units)
                _value = new TimeSpan(Time.ParseDuration((string)value) / 100);
            }
        }

        /// <summary>
        /// Gets the minimum value.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }
    }
}
