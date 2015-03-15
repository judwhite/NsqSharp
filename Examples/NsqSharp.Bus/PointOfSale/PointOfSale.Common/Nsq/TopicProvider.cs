using System;
using NsqSharp.Bus.Configuration.Providers;
using PointOfSale.Messages;

namespace PointOfSale.Common.Nsq
{
    public class TopicProvider : IMessageTypeToTopicProvider
    {
        private readonly Topics _topics = new Topics();

        public string GetTopic(Type messageType)
        {
            return _topics.GetTopic(messageType);
        }
    }
}
