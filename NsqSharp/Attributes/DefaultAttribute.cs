using System;

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
        public DefaultAttribute(object value)
        {
            _value = value;
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
