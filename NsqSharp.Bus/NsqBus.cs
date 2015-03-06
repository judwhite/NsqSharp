using System;
using System.Collections.Generic;
using System.Net;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Converters;
using NsqSharp.Bus.Utils;
using NsqSharp.Go;

namespace NsqSharp.Bus
{
    internal class NsqBus : IBus
    {
        private readonly Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;
        private readonly IObjectBuilder _dependencyInjectionContainer;
        private readonly IMessageTypeToTopicConverter _messageTypeToTopicConverter;
        private readonly IMessageSerializer _sendMessageSerializer;
        private readonly string[] _defaultProducerNsqdHttpEndpoints;

        public NsqBus(
            Dictionary<string, List<MessageHandlerMetadata>> topicChannelHandlers,
            IObjectBuilder dependencyInjectionContainer,
            IMessageTypeToTopicConverter messageTypeToTopicConverter,
            IMessageSerializer sendMessageSerializer,
            string[] defaultProducerNsqdHttpEndpoints
        )
        {
            if (topicChannelHandlers == null)
                throw new ArgumentNullException("topicChannelHandlers");
            if (dependencyInjectionContainer == null)
                throw new ArgumentNullException("dependencyInjectionContainer");
            if (messageTypeToTopicConverter == null)
                throw new ArgumentNullException("messageTypeToTopicConverter");
            if (sendMessageSerializer == null)
                throw new ArgumentNullException("sendMessageSerializer");
            if (defaultProducerNsqdHttpEndpoints == null)
                throw new ArgumentNullException("defaultProducerNsqdHttpEndpoints");
            if (defaultProducerNsqdHttpEndpoints.Length == 0)
                throw new ArgumentException("must contain elements", "defaultProducerNsqdHttpEndpoints");

            _topicChannelHandlers = topicChannelHandlers;
            _dependencyInjectionContainer = dependencyInjectionContainer;
            _messageTypeToTopicConverter = messageTypeToTopicConverter;
            _sendMessageSerializer = sendMessageSerializer;

            _defaultProducerNsqdHttpEndpoints = new string[defaultProducerNsqdHttpEndpoints.Length];
            for (int i = 0; i < defaultProducerNsqdHttpEndpoints.Length; i++)
            {
                string endpoint = defaultProducerNsqdHttpEndpoints[i];
                if (!endpoint.StartsWith("http://"))
                    endpoint = string.Format("http://{0}", endpoint);

                string pingEndpoint = string.Format("{0}/ping", endpoint);
                try
                {
                    var webClient = new WebClient();
                    string result = webClient.DownloadString(pingEndpoint);

                    if (result != "OK")
                    {
                        throw new Exception(string.Format("{0} returned {1}", pingEndpoint, result));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Error connecting to {0}", pingEndpoint), ex);
                }

                _defaultProducerNsqdHttpEndpoints[i] = endpoint;
            }

            _dependencyInjectionContainer.Inject((IBus)this);
        }

        private string GetTopic<T>()
        {
            return _messageTypeToTopicConverter.GetTopic(typeof(T));
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

        public void Send<T>(T message, params string[] nsqdHttpAddresses)
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
        }

        public void Send<T>(T message, string topic, params string[] nsqdHttpAddresses)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");

            byte[] serializedMessage = _sendMessageSerializer.Serialize(message);

            if (nsqdHttpAddresses == null)
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

        public void Send<T>(string topic, params string[] nsqdHttpAddresses)
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
        }

        /*public void Defer<T>(TimeSpan delay, T message)
        {
            throw new NotImplementedException();
        }

        public void Defer<T>(DateTime processAt, T message)
        {
            throw new NotImplementedException();
        }*/

        public IMessage CurrentMessage
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

        public T CreateInstance<T>()
        {
            return _dependencyInjectionContainer.GetInstance<T>();
        }

        public void Start()
        {
            foreach (var topicChannelHandler in _topicChannelHandlers)
            {
                foreach (var item in topicChannelHandler.Value)
                {
                    Consumer consumer = new Consumer(item.Topic, item.Channel, item.Config);
                    //consumer.SetLogger(); // TODO
                    // TODO: max_in_flight vs item.InstanceCount
                    consumer.AddConcurrentHandlers(new MessageDistributor(_dependencyInjectionContainer, item), item.InstanceCount);
                    item.Consumer = consumer;

                    consumer.ConnectToNSQLookupds(item.NsqLookupdHttpAddresses);

                    // TODO: Start consumers.
                }
            }
        }

        public void Stop()
        {
            // TODO: Graceful shutdown
            // TODO: Stop all producers, consumers
        }
    }
}
