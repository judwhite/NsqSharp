using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using NsqSharp;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Loggers;

namespace Echo.Bus.Tls
{
    class Program
    {
        static void Main()
        {
            // Run nsqd using TLS:
            // 
            // nsqd -tls-cert="cert.pem" -tls-key="privatekey.pem" -tls-required=1 -https-address=0.0.0.0:4152 -lookupd-tcp-address=127.0.0.1:4160
            // 
            // See https://github.com/judwhite/NsqSharp/wiki/Securing-NSQ-with-TLS-and-Auth for more info
            // 
            // Self-signed test keys are in the ./test-keys folder in this project.
            // In order to use self-signed keys TlsConfig.InsecureSkipVerify must be set to true.

            // new dependency injection container
            var container = new ContainerBuilder().Build();

            var nsqConfig = new Config
                            {
                                TlsConfig = new TlsConfig
                                            {
                                                MinVersion = SslProtocols.Tls12,
                                                InsecureSkipVerify = true // NOTE: For testing only
                                            },
                                LookupdPollInterval = TimeSpan.FromSeconds(5),
                                DialTimeout = TimeSpan.FromSeconds(10)
                            };

            // start the bus
            BusService.Start(new BusConfiguration(
                new AutofacObjectBuilder(container), // dependency injection container
                new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly), // message serializer
                new ConsoleMessageAuditor(), // receives received, started, and failed notifications
                new MessageTypeToTopicDictionary( // mapping between .NET message types and topics
                    new Dictionary<Type, string> {
                        { typeof(EchoMessage), "echo-topic-tls" },
                        { typeof(ReverseEchoMessage), "reverse-echo-topic-tls" }
                    }
                ),
                new HandlerTypeToChannelDictionary( // mapping between IHandleMessages<T> implementations and channels
                    new Dictionary<Type, string> {
                        { typeof(EchoMessageHandler), "echo-channel-tls" },
                        { typeof(ReverseEchoMessageHandler), "reverse-echo-channel-tls" }
                    }
                ),
                busStateChangedHandler: new BusStateChangedHandler(),
                defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                nsqLogger: new ConsoleLogger(Core.LogLevel.Info), // default = TraceLogger
                defaultThreadsPerHandler: 1, // threads per handler. tweak based on use case.
                nsqConfig: nsqConfig
            ));
        }
    }

    // respond to bus state changes

    public class BusStateChangedHandler : IBusStateChangedHandler
    {
        public void OnBusStarting(IBusConfiguration config) { }
        public void OnBusStopping(IBusConfiguration config, IBus bus) { }
        public void OnBusStopped(IBusConfiguration config) { }

        public void OnBusStarted(IBusConfiguration config, IBus bus)
        {
            // called synchronously, don't block here
            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Enter your message (^C to quit):");
                while (true)
                {
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                        bus.Send(new EchoMessage { Text = line });
                }
            });
        }
    }

    // message types

    public class EchoMessage { public string Text { get; set; } }
    public class ReverseEchoMessage { public string Text { get; set; } }

    // message handlers

    public class EchoMessageHandler : IHandleMessages<EchoMessage>
    {
        private readonly IBus _bus;

        // the bus is registered with the DI container and can be injected into a handler's constructor
        public EchoMessageHandler(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(EchoMessage message)
        {
            Console.WriteLine("Echo: {0}", message.Text);

            // send a new message on the bus
            _bus.Send(new ReverseEchoMessage { Text = message.Text });
        }
    }

    public class ReverseEchoMessageHandler : IHandleMessages<ReverseEchoMessage>
    {
        public void Handle(ReverseEchoMessage message)
        {
            Console.WriteLine("Reverse Echo: {0}", new string(message.Text.Reverse().ToArray()));
        }
    }
}
