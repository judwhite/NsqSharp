using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Configuration.Providers;

namespace PointOfSale.Common
{
    public static class PointOfSaleBus
    {
        public static void Start(
            IHandlerTypeToChannelProvider channelProvider,
            IEnumerable<Assembly> handlerAssemblies,
            IBusStateChangedHandler busStateChangedHandler = null
        )
        {
            if (channelProvider == null)
                throw new ArgumentNullException("channelProvider");

            // http://www.thomas-bayer.com/sqlrest/

            var config = new BusConfiguration(
                new StructureMapObjectBuilder(ObjectFactory.Container),
                new NewtonsoftJsonSerializer(typeof(JsonConvert).Assembly),
                new FailedMessageHandler(),
                new TopicProvider(),
                channelProvider,
                defaultThreadsPerHandler: 8,
                defaultNsqlookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                busStateChangedHandler: busStateChangedHandler
            );

            config.AddMessageHandlers(handlerAssemblies);

            BusService.Start(config);
        }
    }
}
