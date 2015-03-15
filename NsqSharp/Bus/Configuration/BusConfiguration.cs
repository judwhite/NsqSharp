using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Logging;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Loggers;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Configure and start a new Bus.
    /// </summary>
    public class BusConfiguration : IBusConfiguration
    {
        private readonly Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;

        private readonly IObjectBuilder _dependencyInjectionContainer;
        private readonly IMessageSerializer _defaultMessageSerializer;
        private readonly IMessageAuditor _messageAuditor;
        private readonly string[] _defaultNsqlookupdHttpEndpoints;
        private readonly Config _defaultConsumerNsqConfig;
        private readonly int _defaultThreadsPerHandler;
        private readonly IMessageTypeToTopicProvider _messageTypeToTopicProvider;
        private readonly IHandlerTypeToChannelProvider _handlerTypeToChannelProvider;
        private readonly string[] _defaultNsqdHttpEndpoints;
        private readonly IBusStateChangedHandler _busStateChangedHandler;
        private readonly ILogger _nsqLogger;

        private NsqBus _bus;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusConfiguration"/> class.
        /// </summary>
        /// <param name="dependencyInjectionContainer">The DI container to use for this bus (required). See
        /// <see cref="StructureMapObjectBuilder"/> for a default implementation.</param>
        /// <param name="defaultMessageSerializer">The default message serializer/deserializer. See
        /// <see cref="NewtonsoftJsonSerializer" /> for a default implementation.</param>
        /// <param name="messageAuditor">The handler to call when a message fails to process.</param>
        /// <param name="messageTypeToTopicProvider">The message type to topic provider.</param>
        /// <param name="handlerTypeToChannelProvider">The handler type to channel provider.</param>
        /// <param name="defaultNsqlookupdHttpEndpoints">The default nsqlookupd HTTP endpoints; typically listening
        /// on port 4161.</param>
        /// <param name="defaultThreadsPerHandler">The default number of threads per message handler.</param>
        /// <param name="defaultConsumerNsqConfig">The default NSQ Consumer <see cref="Config"/> (optional).</param>
        /// <param name="busStateChangedHandler">Handle bus start and stop events (optional).</param>
        /// <param name="nsqLogger">The <see cref="ILogger"/> used by NsqSharp when communicating with nsqd/nsqlookupd.
        /// (default = <see cref="TraceLogger"/>).</param>
        public BusConfiguration(
            IObjectBuilder dependencyInjectionContainer,
            IMessageSerializer defaultMessageSerializer,
            IMessageAuditor messageAuditor,
            IMessageTypeToTopicProvider messageTypeToTopicProvider,
            IHandlerTypeToChannelProvider handlerTypeToChannelProvider,
            string[] defaultNsqlookupdHttpEndpoints,
            int defaultThreadsPerHandler,
            Config defaultConsumerNsqConfig = null,
            IBusStateChangedHandler busStateChangedHandler = null,
            ILogger nsqLogger = null
        )
        {
            if (dependencyInjectionContainer == null)
                throw new ArgumentNullException("dependencyInjectionContainer");
            if (defaultMessageSerializer == null)
                throw new ArgumentNullException("defaultMessageSerializer");
            if (messageAuditor == null)
                throw new ArgumentNullException("messageAuditor");
            if (messageTypeToTopicProvider == null)
                throw new ArgumentNullException("messageTypeToTopicProvider");
            if (handlerTypeToChannelProvider == null)
                throw new ArgumentNullException("handlerTypeToChannelProvider");
            if (defaultNsqlookupdHttpEndpoints == null)
                throw new ArgumentNullException("defaultNsqlookupdHttpEndpoints");
            if (defaultNsqlookupdHttpEndpoints.Length == 0)
                throw new ArgumentNullException("defaultNsqlookupdHttpEndpoints", "must contain elements");
            if (defaultThreadsPerHandler <= 0)
                throw new ArgumentOutOfRangeException("defaultThreadsPerHandler", "must be > 0");

            _topicChannelHandlers = new Dictionary<string, List<MessageHandlerMetadata>>();

            _messageTypeToTopicProvider = messageTypeToTopicProvider;
            _handlerTypeToChannelProvider = handlerTypeToChannelProvider;

            _dependencyInjectionContainer = dependencyInjectionContainer;
            _defaultMessageSerializer = defaultMessageSerializer;
            _messageAuditor = messageAuditor;
            _defaultNsqlookupdHttpEndpoints = defaultNsqlookupdHttpEndpoints;
            _defaultConsumerNsqConfig = defaultConsumerNsqConfig ?? new Config();
            _defaultThreadsPerHandler = defaultThreadsPerHandler;
            _defaultNsqdHttpEndpoints = new[] { "127.0.0.1:4151" };
            _busStateChangedHandler = busStateChangedHandler;
            _nsqLogger = nsqLogger ?? new TraceLogger();

            var handlerTypes = _handlerTypeToChannelProvider.GetHandlerTypes();
            AddMessageHandlers(handlerTypes);
        }

        /// <summary>
        /// Add message handlers from the specified list of <paramref name="handlerTypes"/>.
        /// Uses defaults specified in the <see cref="BusConfiguration"/> constructor.
        /// </summary>
        /// <param name="handlerTypes">The message handler types to add. Throws if a type is an invalid message handler.</param>
        private void AddMessageHandlers(IEnumerable<Type> handlerTypes)
        {
            if (handlerTypes == null)
                throw new ArgumentNullException("handlerTypes");

            foreach (var handlerType in handlerTypes)
            {
                Type messageType;
                if (IsMessageHandler(handlerType, out messageType))
                {
                    string topic;
                    try
                    {
                        topic = _messageTypeToTopicProvider.GetTopic(messageType);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format(
                            "Topic for message type '{0}' not registered.", messageType.FullName), ex);
                    }

                    string channel = _handlerTypeToChannelProvider.GetChannel(handlerType);
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
            if (_bus != null)
                throw new Exception("Handlers can only be added before the bus is started");

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
                NsqLookupdHttpAddresses = nsqLookupdHttpAddresses,
                Serializer = messageSerializer ?? _defaultMessageSerializer,
                MessageAuditor = _messageAuditor,
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
            // Pre-create topics/channels
            // TODO: make this configurable

            var wg = new WaitGroup();
            foreach (var tch in GetHandledTopics())
            {
                foreach (var nsqdHttpAddress in _defaultNsqdHttpEndpoints)
                {
                    foreach (var channel in tch.Channels)
                    {
                        string localNsqdHttpAddress = nsqdHttpAddress;
                        string localTopic = tch.Topic;
                        string localChannel = channel;

                        wg.Add(1);
                        GoFunc.Run(() =>
                        {
                            try
                            {
                                NsqdHttpApi.CreateTopic(localNsqdHttpAddress, localTopic);
                                NsqdHttpApi.CreateChannel(localNsqdHttpAddress, localTopic, localChannel);
                            }
                            catch (Exception)
                            {
                                // TODO: Log
                            }

                            wg.Done();
                        }, "BusConfiguration pre-create topics/channels");
                    }
                }
            }

            wg.Wait();

            if (_busStateChangedHandler != null)
                _busStateChangedHandler.OnBusStarting(this);

            _bus = new NsqBus(
                _topicChannelHandlers,
                _dependencyInjectionContainer,
                _messageTypeToTopicProvider,
                _defaultMessageSerializer,
                _defaultNsqdHttpEndpoints,
                _nsqLogger
            );

            _bus.Start();

            if (_busStateChangedHandler != null)
                _busStateChangedHandler.OnBusStarted(this, _bus);
        }

        internal void StopBus()
        {
            if (_busStateChangedHandler != null)
                _busStateChangedHandler.OnBusStopping(this, _bus);

            _bus.Stop();

            if (_busStateChangedHandler != null)
                _busStateChangedHandler.OnBusStopped(this);
        }

        /// <summary>
        /// Gets a list of topics/channels currently handled by this process.
        /// </summary>
        /// <returns>A list of topics/channels currently handled by this process.</returns>
        public Collection<ITopicChannels> GetHandledTopics()
        {
            var list = new Collection<ITopicChannels>();
            foreach (var tch in _topicChannelHandlers)
            {
                var topic = tch.Value.Select(p => p.Topic).Distinct().Single();

                var channels = new Collection<string>(
                    tch.Value.Select(p => p.Channel).ToList()
                );

                list.Add(new TopicChannels { Topic = topic, Channels = channels });
            }

            return new Collection<ITopicChannels>(list);
        }

        /// <summary>
        /// <c>true</c> if the process is running in a console window.
        /// </summary>
        public bool IsConsoleMode
        {
            get { return (BusService.GetConsoleWindow() != IntPtr.Zero); }
        }
    }

    /// <summary>
    /// Bus configuration.
    /// </summary>
    public interface IBusConfiguration
    {
        /// <summary>
        /// Gets a list of topics/channels currently handled by this process.
        /// </summary>
        /// <returns>A list of topics/channels currently handled by this process.</returns>
        Collection<ITopicChannels> GetHandledTopics();

        /// <summary>
        /// <c>true</c> if the process is running in a console window.
        /// </summary>
        bool IsConsoleMode { get; }
    }
}
