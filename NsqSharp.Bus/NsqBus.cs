using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Utils;
using NsqSharp.Go;
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

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message);
        }

        /*public void Send<T>(T message, params string[] nsqdHttpAddresses)
        {
            Send(message, GetTopic<T>(), nsqdHttpAddresses);
        }

        public void Send<T>(params string[] nsqdHttpAddresses)
        {
            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            Send(message, nsqdHttpAddresses);
        }

        public void Send<T>(Action<T> messageConstructor, params string[] nsqdHttpAddresses)
        {
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message, nsqdHttpAddresses);
        }*/

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

            // TODO: Re-use Producers per nsqd/topic/thread
            foreach (var nsqdAddress in nsqdHttpAddresses)
            {
                // NOTE: WebClient instance methods are not thread safe
                string publishAddress = string.Format("{0}/pub?topic={1}", nsqdAddress, topic);

                // TODO: What happens if this call fails? Error code or exception? Logging?
                WebClient webClient = new WebClient();
                webClient.UploadData(publishAddress, serializedMessage);
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

        /*public void Send<T>(string topic, params string[] nsqdHttpAddresses)
        {
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            Send(message, topic, nsqdHttpAddresses);
        }

        public void Send<T>(Action<T> messageConstructor, string topic, params string[] nsqdHttpAddresses)
        {
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message, topic, nsqdHttpAddresses);
        }*/

        /*public void Defer<T>(TimeSpan delay, T message)
        {
            throw new NotImplementedException();
        }

        public void Defer<T>(DateTime processAt, T message)
        {
            throw new NotImplementedException();
        }*/

        public Message CurrentMessage
        {
            get { throw new NotImplementedException(); }
        }

        public void SendLocal<T>(T message)
        {
            throw new NotImplementedException();
        }

        public void SendLocal<T>()
        {
            SendLocal<T>(mc => { });
        }

        public void SendLocal<T>(Action<T> messageConstructor)
        {
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            SendLocal(message);
        }

        private T CreateInstance<T>()
        {
            return typeof(T).IsInterface
                        ? InterfaceBuilder.Create<T>()
                        : _dependencyInjectionContainer.GetInstance<T>();
        }

        public void Start()
        {
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
        }

        public void Stop()
        {
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

            // TODO: Stop all producers
        }
    }
}
