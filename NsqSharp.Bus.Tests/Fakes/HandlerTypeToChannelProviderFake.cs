using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;

namespace NsqSharp.Bus.Tests.Fakes
{
    public class HandlerTypeToChannelProviderFake : IHandlerTypeToChannelProvider
    {
        private readonly Dictionary<Type, string> _handlerChannels;

        public HandlerTypeToChannelProviderFake(IEnumerable<KeyValuePair<Type, string>> handlerChannels)
        {
            _handlerChannels = new Dictionary<Type, string>();
            foreach (var kvp in handlerChannels)
            {
                _handlerChannels.Add(kvp.Key, kvp.Value);
            }
        }

        public string GetChannel(Type handlerType)
        {
            return _handlerChannels[handlerType];
        }

        public IEnumerable<Type> GetHandlerTypes()
        {
            return _handlerChannels.Keys;
        }
    }
}
