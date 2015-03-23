using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Logging;
using NsqSharp.Core;
using NsqSharp.Utils.Loggers;
using StructureMap;

namespace Echo.Bus
{
    class Program
    {
        static void Main()
        {
            var container = new Container();

            // start the bus
            BusService.Start(new BusConfiguration(
                new StructureMapObjectBuilder(container), // dependency injection container
                new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly), // message serializer
                new MessageAuditor(), // receives received, started, and failed notifications
                new MessageTypeToTopicDictionary( // mapping between .NET message types and topics
                    new Dictionary<Type, string> {
                        { typeof(EchoMessage), "echo-topic" },
                        { typeof(ReverseEchoMessage), "reverse-echo-topic" }
                    }
                ),
                new HandlerTypeToChannelDictionary( // mapping between IHandleMessages<T> implementations and channels
                    new Dictionary<Type, string> {
                        { typeof(EchoMessageHandler), "echo-channel" },
                        { typeof(ReverseEchoMessageHandler), "reverse-echo-channel" }
                    }
                ),
                busStateChangedHandler: new BusStateChangedHandler(),
                defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                nsqLogger: new ConsoleLogger(LogLevel.Info), 
                defaultThreadsPerHandler: 1, // threads per handler. tweak based on use case.
                preCreateTopicsAndChannels: true // pre-create topics so we dont have to wait for an nsqlookupd cycle
            ));
        }
    }

    public class BusStateChangedHandler : IBusStateChangedHandler
    {
        public void OnBusStarting(IBusConfiguration config) { }

        public void OnBusStarted(IBusConfiguration config, IBus bus)
        {
            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Enter your message (^C to quit):");

                // Get user input
                while (true)
                {
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                        bus.Send(new EchoMessage { Text = line });
                }
            });
        }

        public void OnBusStopping(IBusConfiguration config, IBus bus) { }

        public void OnBusStopped(IBusConfiguration config) { }
    }

    public class EchoMessage
    {
        public string Text { get; set; }
    }

    public class ReverseEchoMessage
    {
        public string Text { get; set; }
    }

    public class EchoMessageHandler : IHandleMessages<EchoMessage>
    {
        private readonly IBus _bus;

        public EchoMessageHandler(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(EchoMessage message)
        {
            Console.WriteLine("Echo: {0}", message.Text);
            _bus.Send(new ReverseEchoMessage { Text = message.Text });
        }
    }

    public class ReverseEchoMessageHandler : IHandleMessages<ReverseEchoMessage>
    {
        public void Handle(ReverseEchoMessage message)
        {
            Console.WriteLine("Reverse Echo: {0}", new String(message.Text.Reverse().ToArray()));
        }
    }

    public class MessageAuditor : IMessageAuditor
    {
        public void OnReceived(IBus bus, IMessageInformation info)
        {
            // log received message
        }

        public void OnSucceeded(IBus bus, IMessageInformation info)
        {
            // log successful message
        }

        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo)
        {
            // log failed message
            Console.WriteLine(failedInfo.Exception);
        }
    }
}
