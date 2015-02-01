using System;

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
        public MinAttribute(object value)
        {
            _value = value;
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
