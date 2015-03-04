using System;
using System.Collections.Generic;
using NsqSharp.Bus.Utils;

namespace NsqSharp.Bus.Configuration.Converters
{
    internal interface IMessageTypeToTopicConverter
    {
        string GetTopic(Type messageType);
    }

    internal class MessageTypeToTopicConverter : IMessageTypeToTopicConverter
    {
        private readonly Dictionary<Type, string> _messageTopics = new Dictionary<Type, string>();
        private readonly object _messageTopicsLocker = new object();

        public string GetTopic(Type messageType)
        {
            if (messageType == null)
                throw new ArgumentNullException("messageType");

            lock (_messageTopicsLocker)
            {
                string topicName;
                if (_messageTopics.TryGetValue(messageType, out topicName))
                    return topicName;

                topicName = string.Format("{0}.{1}", messageType.Namespace, messageType.Name);
                if (topicName.Length > 62)
                {
                    string crc32 = Crc32.Calculate(topicName);
                    string shortName = topicName.Substring(topicName.Length - 64 + 9);
                    int dotIdx = shortName.IndexOf('.');
                    if (dotIdx != -1)
                        shortName = shortName.Substring(dotIdx + 1);
                    topicName = string.Format("{0}-{1}", shortName, crc32);
                }

                topicName = topicName.Replace('.', '-');

                _messageTopics.Add(messageType, topicName);

                return topicName;
            }
        }
    }
}
