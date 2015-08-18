using System;

namespace NsqMon.Common.ApplicationServices
{
    /// <summary>
    /// IEventAggregator
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Subscribes a handler to message with the specified payload type.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="handler">The handler.</param>
        void Subscribe<T>(Action<T> handler);

        /// <summary>
        /// Publishes the specified payload.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="payload">The payload.</param>
        void Publish<T>(T payload);
    }
}
