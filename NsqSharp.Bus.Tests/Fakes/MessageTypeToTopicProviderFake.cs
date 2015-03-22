using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;

namespace NsqSharp.Bus.Tests.Fakes
{
    public class MessageTypeToTopicProviderFake : IMessageTypeToTopicProvider
    {
        private readonly Dictionary<Type, string> _messageTopics;

        public MessageTypeToTopicProviderFake(IEnumerable<KeyValuePair<Type, string>> messageTopics)
        {
            _messageTopics = new Dictionary<Type, string>();
            foreach (var kvp in messageTopics)
            {
                _messageTopics.Add(kvp.Key, kvp.Value);
            }
        }

        public string GetTopic(Type messageType)
        {
            return _messageTopics[messageType];
        }
    }
}
