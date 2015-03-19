using System;
using System.Collections.Generic;
using NsqSharp.Bus.Configuration.Providers;
using PingPong.Handlers;

namespace PingPong.Configuration.Mappings
{
    public class HandlerTypeToChannelProvider : IHandlerTypeToChannelProvider
    {
        // every handler maps to a channel off a topic.
        // channels are independent listeners to the stream of messages sent to a topic.

        // a handler is an implementation of IHandleMessages<T>.

        private readonly Dictionary<Type, string> _handlerToChannel = new Dictionary<Type, string>();

        public HandlerTypeToChannelProvider()
        {
            _handlerToChannel.Add(typeof(PingHandler), "ping-handler");
            _handlerToChannel.Add(typeof(PongHandler), "pong-handler");
        }

        public string GetChannel(Type handlerType)
        {
            return _handlerToChannel[handlerType];
        }

        public IEnumerable<Type> GetHandlerTypes()
        {
            return _handlerToChannel.Keys;
        }
    }
}
