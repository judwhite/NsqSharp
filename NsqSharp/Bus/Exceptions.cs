using System;
using System.Runtime.Serialization;

namespace NsqSharp.Bus
{
    /// <summary>
    /// Thrown when a message handler configuration is invalid.
    /// </summary>
    [Serializable]
    public class HandlerConfigurationException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="HandlerConfigurationException"/> class.</summary>
        /// <param name="message">The exception message.</param>
        public HandlerConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerConfigurationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected HandlerConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
