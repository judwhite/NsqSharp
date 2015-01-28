using System;
using NsqSharp.Go;

namespace NsqSharp.Attributes
{
    /// <summary>
    /// Default value attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DefaultAttribute : Attribute
    {
        private readonly object _value;

        /// <summary>
        /// Initializes a new instance of the DefaultAttribute class.
        /// </summary>
        /// <param name="value">The default value.</param>
        public DefaultAttribute(string value)
            : this((object)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultAttribute class.
        /// </summary>
        /// <param name="value">The default value.</param>
        /// <param name="isDuration">Indicates if the value should be parsed as a duration</param>
        public DefaultAttribute(string value, bool isDuration)
            : this((object)value, isDuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultAttribute class.
        /// </summary>
        /// <param name="value">The default value.</param>
        public DefaultAttribute(ushort value)
            : this((object)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultAttribute class.
        /// </summary>
        /// <param name="value">The default value.</param>
        public DefaultAttribute(int value)
            : this((object)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DefaultAttribute class.
        /// </summary>
        /// <param name="value">The default value.</param>
        public DefaultAttribute(double value)
            : this((object)value)
        {
        }

        private DefaultAttribute(object value, bool isDuration = false)
        {
            _value = value;

            if (isDuration)
            {
                // convert from nanoseconds to ticks (100-nanosecond units)
                _value = new TimeSpan(Time.ParseDuration((string)value) / 100);
            }
        }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }
    }
}
