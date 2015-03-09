using System;
using System.Collections.Generic;
using NsqSharp;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration.Providers;

namespace PointOfSale.Common
{
    public abstract class ChannelProviderBase : IHandlerTypeToChannelProvider
    {
        private readonly Dictionary<Type, string> _channels;
        private readonly List<string> _messageTypeChannels;

        protected ChannelProviderBase()
        {
            _channels = new Dictionary<Type, string>();
            _messageTypeChannels = new List<string>();
        }

        public string GetChannel(Type handlerType)
        {
            return _channels[handlerType];
        }

        public Dictionary<Type, string> GetAll()
        {
            return _channels;
        }

        public void Add<THandler, TMessageType>(string channelName)
            where THandler : IHandleMessages<TMessageType>
        {
            if (string.IsNullOrEmpty(channelName))
                throw new ArgumentNullException("channelName");
            if (!Protocol.IsValidChannelName(channelName))
                throw new ArgumentException("invalid channel name", "channelName");

            _channels.Add(typeof(THandler), channelName);

            // NsqSharp.Bus enforces that a message type can only be produced on a single topic.
            // If we try to add another handler which handles the same message type (and therefore topic) using
            // the same channel name we'll have two handlers competing for the same channel; throw an exception if
            // we accidentally try to do this.

            // TODO: This should be enforced in NsqSharp.Bus

            string messageTypeChannelKey = string.Format("{0}.{1}", typeof(TMessageType).FullName, channelName);

            if (_messageTypeChannels.Contains(messageTypeChannelKey))
            {
                throw new Exception(string.Format("Another handler for message type '{0}' already listening to channel '{1}'.",
                    typeof(TMessageType), channelName));
            }

            _messageTypeChannels.Add(messageTypeChannelKey);
        }
    }
}
