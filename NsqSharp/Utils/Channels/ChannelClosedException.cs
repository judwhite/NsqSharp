using System;
using System.Runtime.Serialization;

namespace NsqSharp.Utils.Channels
{
    /// <summary>
    /// Occurs when attempt to send or receive from a closed channel.
    /// </summary>
    [Serializable]
    public class ChannelClosedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelClosedException"/> class.
        /// </summary>
        public ChannelClosedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelClosedException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ChannelClosedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
