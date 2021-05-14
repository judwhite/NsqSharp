using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;
using PingPong.Messages;

namespace PingPong.Configuration.Mappings
{
    public class MessageTypeToTopicProvider : IMessageTypeToTopicProvider
    {
        // every message type maps to a topic. sending this message type sends to this topic.

        private readonly Dictionary<Type, string> _messageToTopic = new Dictionary<Type, string>();

        public MessageTypeToTopicProvider()
        {
            _messageToTopic.Add(typeof(PingMessage), "pings#ephemeral");
            _messageToTopic.Add(typeof(PongMessage), "pongs#ephemeral");
        }

        public string GetTopic(Type messageType)
        {
            return _messageToTopic[messageType];
        }
    }
}
