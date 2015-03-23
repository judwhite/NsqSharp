using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;
using PointOfSale.Common.Nsq;

namespace PointOfSale.Application.Harness
{
    public class CompositeChannelProvider : IHandlerTypeToChannelProvider
    {
        private readonly Dictionary<Type, string> _channels;

        public CompositeChannelProvider(IEnumerable<ChannelProviderBase> channelProviders)
        {
            if (channelProviders == null)
                throw new ArgumentNullException("channelProviders");

            _channels = new Dictionary<Type, string>();
            foreach (var channelProvider in channelProviders)
            {
                foreach (var handler in channelProvider.GetHandlerTypes())
                {
                    var channel = channelProvider.GetChannel(handler);
                    _channels.Add(handler, channel);
                }
            }
        }

        public string GetChannel(Type handlerType)
        {
            return _channels[handlerType];
        }

        public IEnumerable<Type> GetHandlerTypes()
        {
            return _channels.Keys;
        }
    }
}
