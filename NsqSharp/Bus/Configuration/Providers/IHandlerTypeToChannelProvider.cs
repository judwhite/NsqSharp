using System;
using System.Collections.Generic;

namespace NsqSharp.Bus.Configuration.Providers
{
    /// <summary>
    /// Implement <see cref="IHandlerTypeToChannelProvider"/> to specify which channel a message handler should
    /// consume/subscribe to.
    /// See <see cref="IHandleMessages&lt;T&gt;"/>, <see cref="BusConfiguration"/>,
    /// and <see cref="IMessageTypeToTopicProvider"/>.
    /// </summary>
    public interface IHandlerTypeToChannelProvider
    {
        /// <summary>
        /// Gets the channel the specified <paramref name="handlerType"/> should consume/subscribe to.
        /// </summary>
        /// <param name="handlerType">The message handler type. See <see cref="IHandleMessages&lt;T&gt;"/>.</param>
        /// <returns>The channel the specified <paramref name="handlerType"/> should consume/subscribe to.</returns>
        string GetChannel(Type handlerType);

        /// <summary>
        /// Gets the registered handler types implementing <see cref="IHandleMessages&lt;T&gt;"/>.
        /// </summary>
        /// <returns>The registered handler types implementing <see cref="IHandleMessages&lt;T&gt;"/>.</returns>
        IEnumerable<Type> GetHandlerTypes();
    }
}
