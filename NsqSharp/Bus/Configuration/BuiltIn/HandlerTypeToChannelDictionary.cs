using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>
    /// Convenience class implementing <see cref="IHandlerTypeToChannelProvider"/> with a
    /// <see cref="System.Collections.Generic.Dictionary&lt;Type, String&gt;"/>.
    /// </summary>
    public class HandlerTypeToChannelDictionary : IHandlerTypeToChannelProvider
    {
        private readonly Dictionary<Type, string> _handlerChannels;

        /// <summary>
        /// Initializes a new isntance of the <see cref="HandlerTypeToChannelDictionary"/> class.
        /// </summary>
        /// <param name="handlerChannels">The dictionary of message types to topic names, where Key = handler type,
        /// Value = channel name.</param>
        public HandlerTypeToChannelDictionary(IEnumerable<KeyValuePair<Type, string>> handlerChannels)
        {
            if (handlerChannels == null)
                throw new ArgumentNullException("handlerChannels");

            _handlerChannels = new Dictionary<Type, string>();
            foreach (var kvp in handlerChannels)
            {
                _handlerChannels.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Gets the channel the specified <paramref name="handlerType"/> should consume/subscribe to.
        /// </summary>
        /// <param name="handlerType">The message handler type. See <see cref="IHandleMessages&lt;T&gt;"/>.</param>
        /// <returns>The channel the specified <paramref name="handlerType"/> should consume/subscribe to.</returns>
        public string GetChannel(Type handlerType)
        {
            return _handlerChannels[handlerType];
        }

        /// <summary>
        /// Gets the registered handler types implementing <see cref="IHandleMessages&lt;T&gt;"/>.
        /// </summary>
        /// <returns>The registered handler types implementing <see cref="IHandleMessages&lt;T&gt;"/>.</returns>
        public IEnumerable<Type> GetHandlerTypes()
        {
            return _handlerChannels.Keys;
        }
    }
}
