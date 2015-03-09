using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;
using PointOfSale.Common;

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
                foreach (var kvp in channelProvider.GetAll())
                {
                    _channels.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public string GetChannel(Type handlerType)
        {
            return _channels[handlerType];
        }
    }
}
