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
        private readonly ILogger _nsqLogger;
        private readonly IMessageMutator _messageMutator;
        private readonly IMessageTopicRouter _messageTopicRouter;
        private readonly INsqdPublisher _nsqdPublisher;

        [ThreadStatic]
        private static ICurrentMessageInformation _threadMessage;

        public NsqBus(
            Dictionary<string, List<MessageHandlerMetadata>> topicChannelHandlers,
            IObjectBuilder dependencyInjectionContainer,
            IMessageTypeToTopicProvider messageTypeToTopicProvider,
            IMessageSerializer sendMessageSerializer,
            ILogger nsqLogger,
            IMessageMutator messageMutator,
            IMessageTopicRouter messageTopicRouter,
            INsqdPublisher nsqdPublisher
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
            if (nsqdPublisher == null)
                throw new ArgumentNullException("nsqdPublisher");
            if (nsqLogger == null)
                throw new ArgumentNullException("nsqLogger");

            _topicChannelHandlers = topicChannelHandlers;
            _dependencyInjectionContainer = dependencyInjectionContainer;
            _messageTypeToTopicProvider = messageTypeToTopicProvider;
            _sendMessageSerializer = sendMessageSerializer;
            _nsqLogger = nsqLogger;
            _messageMutator = messageMutator;
            _messageTopicRouter = messageTopicRouter;
            _nsqdPublisher = nsqdPublisher;

            _dependencyInjectionContainer.Inject((IBus)this);
        }

        private string GetTopic(Type t)
        {
            return _messageTypeToTopicProvider.GetTopic(t);
        }
        private string GetTopic<T>()
        {
            return _messageTypeToTopicProvider.GetTopic(typeof(T));
        }

        public void Send(Type messsageType, object message)
        {
            Send(message, GetTopic(messsageType));
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

        private void Send<T>(T message, string topic)
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
            _nsqdPublisher.Publish(topic, serializedMessage);
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
            foreach (var kvp in topicMessages)
            {
                var thisTopic = kvp.Key;
                var thisTopicMessages = kvp.Value;

                // serialize
                var msgByteList = thisTopicMessages.Select(p => _sendMessageSerializer.Serialize(p)).ToList();
                if (msgByteList.Count == 0)
                    continue;

                // send
                _nsqdPublisher.MultiPublish(thisTopic, msgByteList);
            }
        }

        public IMessage CurrentThreadMessage
        {
            get
            {
                var currentMessageInformation = GetCurrentThreadMessageInformation();
                if (currentMessageInformation == null)
                    return null;

                return currentMessageInformation.Message;
            }
        }

        public ICurrentMessageInformation GetCurrentThreadMessageInformation()
        {
            return _threadMessage;
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
                    var consumer = new Consumer(item.Topic, item.Channel, _nsqLogger, item.Config);
                    var distributor = new MessageDistributor(this, _dependencyInjectionContainer, _nsqLogger, item);
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

            _nsqdPublisher.Stop();

            Trace.WriteLine("Stopped.");
        }

        internal void SetCurrentMessageInformation(ICurrentMessageInformation currentMessageInformation)
        {
            _threadMessage = currentMessageInformation;
        }
    }
}
