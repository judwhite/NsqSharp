using System;
using NsqSharp.Go;

namespace NsqSharp.Attributes
{
    /// <summary>
    /// Max value attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MaxAttribute : Attribute
    {
        private readonly object _value;

        /// <summary>
        /// Initializes a new instance of the MaxAttribute class.
        /// </summary>
        /// <param name="value">The maximum value.</param>
        public MaxAttribute(string value)
            : this((object)value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MaxAttribute class.
        /// </summary>
        /// <param name="value">The maximum value.</param>
        public MaxAttribute(int value)
            : this((object)value)
        {
        }

        private MaxAttribute(object value)
        {
            _value = value;

            string strValue = value as string;
            if (strValue != null)
            {
                // convert from nanoseconds to ticks (100-nanosecond units)
                _value = new TimeSpan(Time.ParseDuration(strValue) / 100);
            }
        }

        /// <summary>
        /// Gets the maximum value.
        /// </summary>
        public object Value
        {
            get { return _value; }
        }
    }
}
