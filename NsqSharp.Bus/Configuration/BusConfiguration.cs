using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Configuration.Converters;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Configure and start a new Bus.
    /// </summary>
    public class BusConfiguration
    {
        private readonly Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;

        private readonly IObjectBuilder _dependencyInjectionContainer;
        private readonly IMessageSerializer _defaultMessageSerializer;
        private readonly string[] _defaultNsqlookupdHttpEndpoints;
        private readonly Config _defaultConsumerNsqConfig;
        private readonly int _defaultThreadsPerHandler;
        private readonly IMessageTypeToTopicConverter _messageTypeToTopicCoverter;
        private readonly IHandlerTypeToChannelConverter _handlerTypeToChannelConverter;
        private readonly string[] _defaultNsqdHttpEndpoints;
        private readonly Action _onStart;
        private readonly Action _onStop;

        private NsqBus _bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusConfiguration"/> class.
        /// </summary>
        /// <param name="dependencyInjectionContainer">The DI container to use for this bus (required). See
        /// <see cref="StructureMapObjectBuilder"/> for a default implementation.</param>
        /// <param name="defaultMessageSerializer">The default message serializer/deserializer. See
        /// <see cref="NewtonsoftJsonSerializer" /> for a default implementation.</param>
        /// <param name="defaultNsqlookupdHttpEndpoints">The default nsqlookupd HTTP endpoints; typically listening
        /// on port 4161.</param>
        /// <param name="defaultThreadsPerHandler">The default number of threads per message handler.</param>
        /// <param name="defaultConsumerNsqConfig">The default NSQ Consumer <see cref="Config"/> (optional).</param>
        /// <param name="onStart">Method to call after the bus has started (optional).</param>
        /// <param name="onStop">Method to call after the bus has stopped (optional).</param>
        public BusConfiguration(
            IObjectBuilder dependencyInjectionContainer,
            IMessageSerializer defaultMessageSerializer,
            string[] defaultNsqlookupdHttpEndpoints,
            int defaultThreadsPerHandler,
            Config defaultConsumerNsqConfig = null,
            Action onStart = null,
            Action onStop = null
        )
        {
            if (dependencyInjectionContainer == null)
                throw new ArgumentNullException("dependencyInjectionContainer");
            if (defaultMessageSerializer == null)
                throw new ArgumentNullException("defaultMessageSerializer");
            if (defaultNsqlookupdHttpEndpoints == null)
                throw new ArgumentNullException("defaultNsqlookupdHttpEndpoints");
            if (defaultNsqlookupdHttpEndpoints.Length == 0)
                throw new ArgumentNullException("defaultNsqlookupdHttpEndpoints", "must contain elements");
            if (defaultThreadsPerHandler <= 0)
                throw new ArgumentOutOfRangeException("defaultThreadsPerHandler", "must be > 0");

            _topicChannelHandlers = new Dictionary<string, List<MessageHandlerMetadata>>();

            _messageTypeToTopicCoverter = new MessageTypeToTopicConverter();
            _handlerTypeToChannelConverter = new HandlerTypeToChannelConverter();

            _dependencyInjectionContainer = dependencyInjectionContainer;
            _defaultMessageSerializer = defaultMessageSerializer;
            _defaultNsqlookupdHttpEndpoints = defaultNsqlookupdHttpEndpoints;
            _defaultConsumerNsqConfig = defaultConsumerNsqConfig ?? new Config();
            _defaultThreadsPerHandler = defaultThreadsPerHandler;
            _defaultNsqdHttpEndpoints = new[] { "127.0.0.1:4151" };
            _onStart = onStart;
            _onStop = onStop;
        }

        /// <summary>
        /// Add message handlers by scanning the specified <paramref name="assemblies"/> for implementations of
        /// <see cref="IHandleMessages&lt;T&gt;"/>. Uses default topic/channel naming and defaults specified in
        /// the <see cref="BusConfiguration"/> constructor.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan.</param>
        public void AddMessageHandlers(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
                throw new ArgumentNullException("assemblies");

            var types = assemblies.SelectMany(p => p.GetTypes());

            var handlerTypes = new List<Type>();
            foreach (var type in types)
            {
                Type messageType;
                if (IsMessageHandler(type, out messageType))
                {
                    handlerTypes.Add(type);
                }
            }

            AddMessageHandlers(handlerTypes.ToArray());
        }

        /// <summary>
        /// Add message handlers from the specifieid list of <paramref name="handlerTypes"/>.
        /// Uses default topic/channel naming and defaults specified in the <see cref="BusConfiguration"/> constructor.
        /// </summary>
        /// <param name="handlerTypes">The message handler types to add. Throws if a type is an invalid message handler.</param>
        public void AddMessageHandlers(IEnumerable<Type> handlerTypes)
        {
            if (handlerTypes == null)
                throw new ArgumentNullException("handlerTypes");

            foreach (var handlerType in handlerTypes)
            {
                Type messageType;
                if (IsMessageHandler(handlerType, out messageType))
                {
                    string topic = _messageTypeToTopicCoverter.GetTopic(messageType);
                    string channel = _handlerTypeToChannelConverter.GetChannel(handlerType);
                    AddMessageHandler(handlerType, messageType, topic, channel);
                }
                else
                {
                    throw new Exception(string.Format("Type '{0}' is not a valid message handler", handlerType.FullName));
                }
            }
        }

        /// <summary>
        /// Adds a message handler. If a duplicate <paramref name="topic"/>, <paramref name="channel"/>, and
        /// <typeparamref name="THandler" /> is added the old value will be overwritten.
        /// </summary>
        /// <typeparam name="THandler">The concrete message handler type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="topic">The topic name.</param>
        /// <param name="channel">The channel name.</param>
        /// <param name="messageSerializer">The message serializer (optional; otherwise uses default).</param>
        /// <param name="config">The Consumer <see cref="Config"/> to use (optional; otherwise uses default).</param>
        /// <param name="threadsPerHandler">The number of threads per message handler (optional; otherwise uses default).</param>
        /// <param name="nsqLookupdHttpAddresses">The nsqlookupd HTTP addresses to use (optional; otherwise uses default).</param>
        public void AddMessageHandler<THandler, TMessage>(string topic, string channel,
            IMessageSerializer messageSerializer = null,
            Config config = null,
            int? threadsPerHandler = null,
            params string[] nsqLookupdHttpAddresses
        )
            where THandler : IHandleMessages<TMessage>
        {
            AddMessageHandler(typeof(THandler), typeof(Message), topic, channel,
                messageSerializer, config, threadsPerHandler, nsqLookupdHttpAddresses);
        }

        private void AddMessageHandler(Type handlerType, Type messageType, string topic, string channel,
            IMessageSerializer messageSerializer = null,
            Config config = null,
            int? threadsPerHandler = null,
            params string[] nsqLookupdHttpAddresses
        )
        {
            if (!Protocol.IsValidTopicName(topic))
                throw new ArgumentException(string.Format("'{0}' is not a valid topic name", topic), "topic");
            if (!Protocol.IsValidChannelName(channel))
                throw new ArgumentException(string.Format("'{0}' is not a valid channel name", topic), "channel");

            if (handlerType.IsInterface || handlerType.IsAbstract)
                throw new ArgumentException("handlerType must be instantiable; cannot be an interface or abstract class", "handlerType");

            if (nsqLookupdHttpAddresses == null || nsqLookupdHttpAddresses.Length == 0)
                nsqLookupdHttpAddresses = _defaultNsqlookupdHttpEndpoints;

            string key = string.Format("{0}/{1}", topic, channel);

            List<MessageHandlerMetadata> list;
            if (!_topicChannelHandlers.TryGetValue(key, out list))
            {
                list = new List<MessageHandlerMetadata>();
                _topicChannelHandlers.Add(key, list);
            }

            // remove duplicates based on topic/channel/handlerType; replace with new values
            foreach (var item in new List<MessageHandlerMetadata>(list))
            {
                if (item.HandlerType == handlerType)
                {
                    list.Remove(item);
                }
            }

            // add topic/channel handler
            var messageHandlerMetadata = new MessageHandlerMetadata
            {
                Topic = topic,
                Channel = channel,
                HandlerType = handlerType,
                MessageType = messageType,
                IHandleMessagesType = typeof(IHandleMessages<>).MakeGenericType(messageType),
                NsqLookupdHttpAddresses = nsqLookupdHttpAddresses,
                Serializer = messageSerializer ?? _defaultMessageSerializer,
                Config = config ?? _defaultConsumerNsqConfig,
                InstanceCount = threadsPerHandler ?? _defaultThreadsPerHandler
            };

            list.Add(messageHandlerMetadata);
        }

        /*/// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="nsqdHttpAddresses"></param>
        public void RegisterDestination<TMessage>(string topic, params string[] nsqdHttpAddresses)
        {
            throw new NotImplementedException();
        }*/

        private static bool IsMessageHandler(Type type, out Type messageType)
        {
            messageType = null;

            if (!type.IsClass)
                return false;

            foreach (var interf in type.GetInterfaces())
            {
                if (IsMessageHandlerInterface(interf, out messageType))
                    return true;

                foreach (var subInterf in interf.GetInterfaces())
                {
                    if (IsMessageHandlerInterface(subInterf, out messageType))
                        return true;
                }
            }

            return false;
        }

        private static bool IsMessageHandlerInterface(Type interf, out Type messageType)
        {
            if (interf.IsGenericType && interf.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
            {
                messageType = interf.GetGenericArguments()[0];
                return true;
            }
            messageType = null;
            return false;
        }

        internal void StartBus()
        {
            _bus = new NsqBus(
                _topicChannelHandlers,
                _dependencyInjectionContainer,
                _messageTypeToTopicCoverter,
                _defaultMessageSerializer,
                _defaultNsqdHttpEndpoints
            );

            _bus.Start();

            if (_onStart != null)
                _onStart();
        }

        internal void StopBus()
        {
            _bus.Stop();

            if (_onStop != null)
                _onStop();
        }
    }
}
