using System;

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
        public MaxAttribute(object value)
        {
            _value = value;
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
