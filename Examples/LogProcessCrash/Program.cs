using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Logging;
using StructureMap;
using StructureMap.Graph;

namespace LogProcessCrash
{
    class Program
    {
        private static IContainer _structureMapContainer;

        static void Main()
        {
            _structureMapContainer = new Container();
            _structureMapContainer.Configure(p => p.Scan(x => x.TheCallingAssembly()));

            var messageTopics = new Dictionary<Type, string>();
            messageTopics.Add(typeof(HelloMessage), "HelloMessage");

            var handlerChannels = new Dictionary<Type, string>();
            handlerChannels.Add(typeof(HelloMessageHandler), "LogProcessCrash-Channel");

            // start the bus
            BusService.Start(new BusConfiguration(
                new StructureMapObjectBuilder(_structureMapContainer), // dependency injection container
                new NewtonsoftJsonSerializer(typeof(JsonConvert).Assembly), // message serializer
                new MessageAuditor(), // receives received, started, and failed notifications
                new MessageTypeToTopicProvider( // mapping between .NET message types and topics
                    messageTopics
                ),
                new HandlerTypeToChannelProvider( // mapping between IHandleMessages<T> implementations and channels
                    handlerChannels
                ),
                busStateChangedHandler: new BusStateChangedHandler(), // bus starting/started/stopping/stopped
                preCreateTopicsAndChannels: true, // pre-create topics so we dont have to wait for an nsqlookupd cycle
                defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" }, // nsqlookupd address
                defaultThreadsPerHandler: 1, // threads per handler. tweak based on use case, see handlers in this project.
                logOnProcessCrash: true
            ));
        }
    }

    public class HelloMessage
    {
        public string Text { get; set; }
    }

    public class HelloMessageHandler : IHandleMessages<HelloMessage>
    {
        public void Handle(HelloMessage message)
        {
            if (!message.Text.Contains("die"))
            {
                Trace.WriteLine(message.Text);
            }
            else
            {
                var t = new Thread(() => { throw new Exception(message.Text); });
                t.Start();
            }
        }
    }

    public class BusStateChangedHandler : IBusStateChangedHandler
    {
        public void OnBusStarting(IBusConfiguration config) { }
        public void OnBusStopping(IBusConfiguration config, IBus bus) { }
        public void OnBusStopped(IBusConfiguration config) { }

        /// <summary>
        /// Occurs after the bus starts.
        /// </summary>
        /// <param name="config">The bus configuration.</param>
        /// <param name="bus">The bus.</param>
        public void OnBusStarted(IBusConfiguration config, IBus bus)
        {
            Task.Factory.StartNew(() =>
                                  {
                                      bus.Send(new HelloMessage { Text = "Hello, my name is Inigo Montoya..." });
                                      Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                                      bus.Send(new HelloMessage { Text = "You killed my father..." });
                                      Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                                      bus.Send(new HelloMessage { Text = "Prepare to die!" });
                                  });
        }
    }

    public class MessageTypeToTopicProvider : MessageTypeToTopicDictionary
    {
        public MessageTypeToTopicProvider(Dictionary<Type, string> messageTopics)
            : base(messageTopics)
        {
        }
    }

    public class HandlerTypeToChannelProvider : HandlerTypeToChannelDictionary
    {
        public HandlerTypeToChannelProvider(Dictionary<Type, string> handlerChannels)
            : base(handlerChannels)
        {
        }
    }

    public class MessageAuditor : IMessageAuditor
    {
        public void OnReceived(IBus bus, IMessageInformation info) { }
        public void OnSucceeded(IBus bus, IMessageInformation info) { }
        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo)
        {
            Trace.TraceError(failedInfo.Exception.ToString());
        }
    }
}
