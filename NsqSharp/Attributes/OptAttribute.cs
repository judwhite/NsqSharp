using System;

namespace NsqSharp.Attributes
{
    /// <summary>
    /// Option attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OptAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the OptAttribute class.
        /// </summary>
        /// <param name="name">The option name to apply to the property.</param>
        public OptAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The option name.
        /// </summary>
        public string Name { get; private set; }
    }
}
