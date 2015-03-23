using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>
    /// Convenience class implementing <see cref="IMessageTypeToTopicProvider"/> with a
    /// <see cref="System.Collections.Generic.Dictionary&lt;Type, String&gt;"/>.
    /// </summary>
    public class MessageTypeToTopicDictionary : IMessageTypeToTopicProvider
    {
        private readonly Dictionary<Type, string> _messageTopics;

        /// <summary>
        /// Initializes a new isntance of the <see cref="MessageTypeToTopicDictionary"/> class.
        /// </summary>
        /// <param name="messageTopics">The dictionary of message types to topic names, where Key = message type,
        /// Value = topic name.</param>
        public MessageTypeToTopicDictionary(IEnumerable<KeyValuePair<Type, string>> messageTopics)
        {
            if (messageTopics == null)
                throw new ArgumentNullException("messageTopics");

            _messageTopics = new Dictionary<Type, string>();
            foreach (var kvp in messageTopics)
            {
                _messageTopics.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Gets the topic the specified <paramref name="messageType"/> should be produced/published on.
        /// </summary>
        /// <param name="messageType">The message type. See <see cref="IHandleMessages&lt;T&gt;"/>.</param>
        /// <returns>The topic the specified <paramref name="messageType"/> should be produced/published on.</returns>
        public string GetTopic(Type messageType)
        {
            return _messageTopics[messageType];
        }
    }
}
