using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Utils;
using NsqSharp.Core;
using NsqSharp.Utils;

namespace NsqSharp.Bus
{
    internal class NsqBus : IBus
    {
        private readonly Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;
        private readonly IObjectBuilder _dependencyInjectionContainer;
        private readonly IMessageTypeToTopicProvider _messageTypeToTopicProvider;
        private readonly IMessageSerializer _sendMessageSerializer;
        private readonly string[] _defaultProducerNsqdHttpEndpoints;

        public NsqBus(
            Dictionary<string, List<MessageHandlerMetadata>> topicChannelHandlers,
            IObjectBuilder dependencyInjectionContainer,
            IMessageTypeToTopicProvider messageTypeToTopicProvider,
            IMessageSerializer sendMessageSerializer,
            string[] defaultProducerNsqdHttpEndpoints
        )
        {
            if (topicChannelHandlers == null)
                throw new ArgumentNullException("topicChannelHandlers");
            if (dependencyInjectionContainer == null)
                throw new ArgumentNullException("dependencyInjectionContainer");
            if (messageTypeToTopicProvider == null)
                throw new ArgumentNullException("messageTypeToTopicProvider");
            if (sendMessageSerializer == null)
                throw new ArgumentNullException("sendMessageSerializer");
            if (defaultProducerNsqdHttpEndpoints == null)
                throw new ArgumentNullException("defaultProducerNsqdHttpEndpoints");
            if (defaultProducerNsqdHttpEndpoints.Length == 0)
                throw new ArgumentException("must contain elements", "defaultProducerNsqdHttpEndpoints");

            _topicChannelHandlers = topicChannelHandlers;
            _dependencyInjectionContainer = dependencyInjectionContainer;
            _messageTypeToTopicProvider = messageTypeToTopicProvider;
            _sendMessageSerializer = sendMessageSerializer;

            _defaultProducerNsqdHttpEndpoints = new string[defaultProducerNsqdHttpEndpoints.Length];
            for (int i = 0; i < defaultProducerNsqdHttpEndpoints.Length; i++)
            {
                string endpoint = defaultProducerNsqdHttpEndpoints[i];
                if (!endpoint.StartsWith("http://"))
                    endpoint = string.Format("http://{0}", endpoint);

                try
                {
                    string result = NsqdHttpApi.Ping(endpoint);

                    if (result != "OK")
                    {
                        throw new Exception(string.Format("{0}/ping returned {1}", endpoint, result));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error connecting to {0}/ping", endpoint), ex);
                }

                _defaultProducerNsqdHttpEndpoints[i] = endpoint;
            }

            _dependencyInjectionContainer.Inject((IBus)this);
        }

        private string GetTopic<T>()
        {
            return _messageTypeToTopicProvider.GetTopic(typeof(T));
        }

        public void Send<T>(T message)
        {
            Send(message, GetTopic<T>());
        }

        public void Send<T>()
        {
            Send<T>(mc => { });
        }

        public void Send<T>(Action<T> messageConstructor)
        {
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = CreateInstance<T>();
            messageConstructor(message);

            Send(message);
        }

        private void Send<T>(T message, string topic, params string[] nsqdHttpAddresses)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");

            byte[] serializedMessage = _sendMessageSerializer.Serialize(message);

            if (nsqdHttpAddresses == null || nsqdHttpAddresses.Length == 0)
            {
                nsqdHttpAddresses = _defaultProducerNsqdHttpEndpoints;
            }

            // TODO: When not usig HTTP, re-use Producers per nsqd/topic/thread
            foreach (var nsqdHttpAddress in nsqdHttpAddresses)
            {
                // TODO: What happens if this call fails? Error code or exception? Logging?
                NsqdHttpApi.Publish(nsqdHttpAddress, topic, serializedMessage);
            }
        }

        public void SendMulti<T>(IEnumerable<T> messages)
        {
            if (messages == null)
                throw new ArgumentNullException("messages");

            string topic = GetTopic<T>();
            var nsqdHttpAddresses = _defaultProducerNsqdHttpEndpoints;

            var msgByteList = messages.Select(p => _sendMessageSerializer.Serialize(p)).ToList();

            // TODO: Re-use Producers per nsqd/topic/thread
            foreach (var nsqdAddress in nsqdHttpAddresses)
            {
                NsqdHttpApi.PublishMultiple(nsqdAddress, topic, msgByteList);
            }
        }

        public Message CurrentMessage
        {
            get { throw new NotImplementedException(); }
        }

        private T CreateInstance<T>()
        {
            return typeof(T).IsInterface
                        ? InterfaceBuilder.CreateObject<T>()
                        : _dependencyInjectionContainer.GetInstance<T>();
        }

        public void Start()
        {
            Trace.WriteLine("Starting...");

            foreach (var topicChannelHandler in _topicChannelHandlers)
            {
                foreach (var item in topicChannelHandler.Value)
                {
                    Consumer consumer = new Consumer(item.Topic, item.Channel, item.Config);
                    consumer.SetLogger(new ConsoleLogger(), LogLevel.Warning); // TODO: Configurable
                    consumer.AddConcurrentHandlers(new MessageDistributor(_dependencyInjectionContainer, item), item.InstanceCount);

                    // TODO: max_in_flight vs item.InstanceCount
                    if (item.Config.MaxInFlight < item.InstanceCount)
                    {
                        consumer.ChangeMaxInFlight(item.InstanceCount);
                    }

                    item.Consumer = consumer;

                    consumer.ConnectToNSQLookupds(item.NsqLookupdHttpAddresses);
                }
            }

            Trace.WriteLine("Started.");
        }

        public void Stop()
        {
            Trace.WriteLine("Stopping...");

            // Stop all Consumers

            var wg = new WaitGroup();
            foreach (var topicChannelHandler in _topicChannelHandlers)
            {
                foreach (var item in topicChannelHandler.Value)
                {
                    var consumer = item.Consumer;
                    if (consumer != null)
                    {
                        wg.Add(1);
                        GoFunc.Run(() =>
                        {
                            consumer.Stop(blockUntilStopCompletes: true);
                            wg.Done();
                        });
                    }
                }
            }

            wg.Wait();

            Trace.WriteLine("Stopped.");
        }
    }
}
