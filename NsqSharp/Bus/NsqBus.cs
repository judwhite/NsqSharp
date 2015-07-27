﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Utils;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Logging;

namespace NsqSharp.Bus
{
    internal class NsqBus : IBus
    {
        private readonly Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;
        private readonly IObjectBuilder _dependencyInjectionContainer;
        private readonly IMessageTypeToTopicProvider _messageTypeToTopicProvider;
        private readonly IMessageSerializer _sendMessageSerializer;
        private readonly string[] _defaultProducerNsqdHttpEndpoints;
        private readonly IMessageMutator _messageMutator;
        private readonly IMessageTopicRouter _messageTopicRouter;

        [ThreadStatic]
        private static ICurrentMessageInformation _threadMessage;

        public NsqBus(
            Dictionary<string, List<MessageHandlerMetadata>> topicChannelHandlers,
            IObjectBuilder dependencyInjectionContainer,
            IMessageTypeToTopicProvider messageTypeToTopicProvider,
            IMessageSerializer sendMessageSerializer,
            string[] defaultProducerNsqdHttpEndpoints,
            IMessageMutator messageMutator,
            IMessageTopicRouter messageTopicRouter
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
            _messageMutator = messageMutator;
            _messageTopicRouter = messageTopicRouter;

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

            // mutate
            if (_messageMutator != null)
            {
                message = _messageMutator.GetMutatedMessage(this, message);
            }

            // route
            if (_messageTopicRouter != null)
            {
                topic = _messageTopicRouter.GetMessageTopic(this, topic, message);
            }

            // serialize
            byte[] serializedMessage = _sendMessageSerializer.Serialize(message);

            // send
            if (nsqdHttpAddresses == null || nsqdHttpAddresses.Length == 0)
            {
                nsqdHttpAddresses = _defaultProducerNsqdHttpEndpoints;
            }

            foreach (var nsqdHttpAddress in nsqdHttpAddresses)
            {
                NsqdHttpApi.Publish(nsqdHttpAddress, topic, serializedMessage);
            }
        }

        public void SendMulti<T>(IEnumerable<T> messages)
        {
            if (messages == null)
                throw new ArgumentNullException("messages");

            string topic = GetTopic<T>();

            var messagesList = messages.ToList();

            // mutate
            if (_messageMutator != null)
            {
                var newList = new List<T>();
                foreach (var message in messagesList)
                {
                    var newMessage = _messageMutator.GetMutatedMessage(this, message);
                    newList.Add(newMessage);
                }
                messagesList = newList;
            }

            // route
            var topicMessages = new Dictionary<string, List<T>>();
            if (_messageTopicRouter != null)
            {
                var originalTopicMessageList = new List<T>();
                topicMessages.Add(topic, originalTopicMessageList);
                foreach (var message in messagesList)
                {
                    var newTopic = _messageTopicRouter.GetMessageTopic(this, topic, message);
                    if (newTopic == topic)
                    {
                        originalTopicMessageList.Add(message);
                    }
                    else
                    {
                        List<T> newTopicMessageList;
                        if (!topicMessages.TryGetValue(newTopic, out newTopicMessageList))
                        {
                            newTopicMessageList = new List<T>();
                            topicMessages.Add(newTopic, newTopicMessageList);
                        }

                        newTopicMessageList.Add(message);
                    }
                }
            }
            else
            {
                topicMessages.Add(topic, messagesList);
            }

            // iterate on topic/message partition
            var nsqdHttpAddresses = _defaultProducerNsqdHttpEndpoints;
            foreach (var kvp in topicMessages)
            {
                var thisTopic = kvp.Key;
                var thisTopicMessages = kvp.Value;

                // serialize
                var msgByteList = thisTopicMessages.Select(p => _sendMessageSerializer.Serialize(p)).ToList();
                if (msgByteList.Count == 0)
                    continue;

                // send
                foreach (var nsqdAddress in nsqdHttpAddresses)
                {
                    NsqdHttpApi.PublishMultiple(nsqdAddress, thisTopic, msgByteList);
                }
            }
        }

        public IMessage CurrentThreadMessage
        {
            get
            {
                var currentMessageInformation = GetCurrentMessageInformation();
                if (currentMessageInformation == null)
                    return null;

                return currentMessageInformation.Message;
            }
        }

        public ICurrentMessageInformation GetCurrentThreadMessageInformation()
        {
            return _threadMessage;
        }

        public IMessage CurrentMessage
        {
            get { return CurrentThreadMessage; }
        }

        public ICurrentMessageInformation GetCurrentMessageInformation()
        {
            return GetCurrentThreadMessageInformation();
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
                    var consumer = new Consumer(item.Topic, item.Channel, item.Config);
                    var distributor = new MessageDistributor(this, _dependencyInjectionContainer, item);
                    consumer.AddHandler(distributor, item.InstanceCount);

                    // TODO: max_in_flight vs item.InstanceCount
                    if (item.Config.MaxInFlight < item.InstanceCount)
                    {
                        consumer.ChangeMaxInFlight(item.InstanceCount);
                    }

                    item.Consumer = consumer;

                    consumer.ConnectToNsqLookupd(item.NsqLookupdHttpAddresses);
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
                            consumer.Stop();
                            wg.Done();
                        }, "NsqBus consumer shutdown");
                    }
                }
            }

            wg.Wait();

            Trace.WriteLine("Stopped.");
        }

        internal void SetCurrentMessageInformation(ICurrentMessageInformation currentMessageInformation)
        {
            _threadMessage = currentMessageInformation;
        }
    }
}
