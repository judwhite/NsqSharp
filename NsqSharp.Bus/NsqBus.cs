using System;
using System.Collections.Generic;
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

        public NsqBus(
            Dictionary<string, List<MessageHandlerMetadata>> topicChannelHandlers,
            IObjectBuilder dependencyInjectionContainer,
            IMessageTypeToTopicConverter messageTypeToTopicConverter,
            IMessageSerializer sendMessageSerializer
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

            _topicChannelHandlers = topicChannelHandlers;
            _dependencyInjectionContainer = dependencyInjectionContainer;
            _messageTypeToTopicConverter = messageTypeToTopicConverter;
            _sendMessageSerializer = sendMessageSerializer;

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

        public void Send<T>(T message, params string[] nsqdTcpAddresses)
        {
            Send(message, GetTopic<T>(), nsqdTcpAddresses);
        }

        public void Send<T>(params string[] nsqdTcpAddresses)
        {
            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            Send(message, nsqdTcpAddresses);
        }

        public void Send<T>(Action<T> messageConstructor, params string[] nsqdTcpAddresses)
        {
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message, nsqdTcpAddresses);
        }

        public void Send<T>(T message, string topic, params string[] nsqdTcpAddresses)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");

            byte[] serializedMessage = _sendMessageSerializer.Serialize(message);

            if (nsqdTcpAddresses == null)
            {
                // TODO: Get default from configuration
                nsqdTcpAddresses = new[] { "127.0.0.1:4150" };
            }

            // TODO: Re-use Producers per nsqd/topic/thread
            foreach (var nsqdAddress in nsqdTcpAddresses)
            {
                // TODO: specify Producer config?
                var p = new Producer(nsqdAddress);
                p.Publish(topic, serializedMessage);
                p.Stop(); // TODO: don't do this until the Bus stop, or the Producer disconnects
            }
        }

        public void Send<T>(string topic, params string[] nsqdTcpAddresses)
        {
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            Send(message, topic, nsqdTcpAddresses);
        }

        public void Send<T>(Action<T> messageConstructor, string topic, params string[] nsqdTcpAddresses)
        {
            if (messageConstructor == null)
                throw new ArgumentNullException("messageConstructor");

            T message = (typeof(T).IsInterface ? InterfaceBuilder.Create<T>() : CreateInstance<T>());
            messageConstructor(message);

            Send(message, topic, nsqdTcpAddresses);
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

        private WaitGroup _wg;
        public IBus Start()
        {
            // TODO: Only allow to be called once
            _wg = new WaitGroup();
            _wg.Add(1);
            GoFunc.Run(() =>
            {
                // TODO: meat goes here
            });
            return this;
        }

        public void Stop()
        {
            // TODO: Graceful shutdown
            // TODO: Stop all producers, consumers
        }

        public void Wait()
        {
            _wg.Wait();
        }
    }
}
