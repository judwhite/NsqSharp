﻿using System;
using Newtonsoft.Json;
using NsqSharp;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.WindowService;
using PointOfSale.Common.IoC;

namespace PointOfSale.Common.Nsq
{
    public static class PointOfSaleBus
    {
        public static void Start(
            IHandlerTypeToChannelProvider channelProvider,
            IBusStateChangedHandler busStateChangedHandler = null,
            IMessageTypeToTopicProvider topicProvider = null
        )
        {
            if (channelProvider == null)
                throw new ArgumentNullException("channelProvider");

            var config = new WindowsBusConfiguration(
                new StructureMapObjectBuilder(ObjectFactory.Container),
                new NewtonsoftJsonSerializer(typeof(JsonConvert).Assembly),
                new MessageAuditor(),
                topicProvider ?? new TopicProvider(),
                channelProvider,
                defaultThreadsPerHandler: 8,
                defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                busStateChangedHandler: busStateChangedHandler,
                preCreateTopicsAndChannels: true,
                nsqConfig:
                    new NsqSharp.Config
                    {
                        BackoffStrategy = new FullJitterStrategy(),
                        //MaxRequeueDelay = TimeSpan.FromSeconds(0.1),
                        //MaxBackoffDuration = TimeSpan.FromSeconds(0.2)
                    }
            );

            BusService.Start(config);
        }
    }
}
