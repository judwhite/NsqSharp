using System;
using NsqSharp;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Utils.Loggers;
using PingPong.Configuration;
using PingPong.Configuration.Audit;
using PingPong.Configuration.Mappings;
using PingPong.Messages;
using PingPong.Services;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Pipeline;

namespace PingPong
{
    class Program
    {
        static void Main()
        {
            var container = SetupDependencyInjectionContainer();

            // start the bus
            BusService.Start(new BusConfiguration(
                new ObjectBuilder(container), // dependency injection container
                new MessageSerializer(), // message serializer
                new MessageAuditor(), // receives received, started, and failed notifications
                new MessageTypeToTopicProvider(), // mapping between .NET message types and topics
                new HandlerTypeToChannelProvider(), // mapping between IHandleMessages<T> implementations and channels
                busStateChangedHandler: new BusStateChangedHandler(), // bus starting/started/stopping/stopped
                defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" }, // nsqlookupd address
                defaultThreadsPerHandler: 1, // threads per handler. tweak based on use case, see handlers in this project.
                defaultConsumerNsqConfig: new Config
                                          {
                                              // optional override of default config values
                                              MaxRequeueDelay = TimeSpan.FromSeconds(15),
                                              MaxBackoffDuration = TimeSpan.FromSeconds(2),
                                              MaxAttempts = 2
                                          },
                nsqLogger: new TraceLogger(), // logger for NSQ events (see also ConsoleLogger, or implement your own)
                preCreateTopicsAndChannels: true // pre-create topics so we dont have to wait for an nsqlookupd cycle
            ));

            // BusService.Start blocks until Ctrl+C pressed when in Console mode

            var counter = container.GetInstance<ICounter>();
            Console.WriteLine("{0:#,0} msgs/sec", counter.Current / counter.Elapsed.TotalSeconds);
        }

        public class BusStateChangedHandler : IBusStateChangedHandler
        {
            public void OnBusStarting(IBusConfiguration config) { }
            public void OnBusStopping(IBusConfiguration config, IBus bus) { }
            public void OnBusStopped(IBusConfiguration config) { }

            public void OnBusStarted(IBusConfiguration config, IBus bus)
            {
                // throw the ball!
                bus.Send(new PongMessage { Message = "Hey, Catch!" });
            }
        }

        private static Container SetupDependencyInjectionContainer()
        {
            return new Container(x =>
            {
                x.Scan(scan =>
                {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
                });

                x.For<ICounter>(Lifecycles.Singleton).Use<Counter>();
            });
        }
    }
}
