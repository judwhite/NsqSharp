using System;

namespace NsqSharp.Bus.Configuration.Providers
{
    /// <summary>
    /// Implement <see cref="IMessageTypeToTopicProvider"/> to specify which topic a message type should be
    /// produced/published on.
    /// See <see cref="IHandleMessages&lt;T&gt;"/>, <see cref="BusConfiguration"/>,
    /// and <see cref="IHandlerTypeToChannelProvider"/>.
    /// </summary>
    public interface IMessageTypeToTopicProvider
    {
        /// <summary>
        /// Gets the topic the specified <paramref name="messageType"/> should be produced/published on.
        /// </summary>
        /// <param name="messageType">The message type. See <see cref="IHandleMessages&lt;T&gt;"/>.</param>
        /// <returns>The topic the specified <paramref name="messageType"/> should be produced/published on.</returns>
        string GetTopic(Type messageType);
    }
}
