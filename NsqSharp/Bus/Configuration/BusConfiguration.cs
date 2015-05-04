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
        private Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;

        private IObjectBuilder _dependencyInjectionContainer;
        private IMessageSerializer _defaultMessageSerializer;
        private IMessageAuditor _messageAuditor;
        private string[] _defaultNsqlookupdHttpEndpoints;
        private Config _defaultConsumerNsqConfig;
        private int _defaultThreadsPerHandler;
        private IMessageTypeToTopicProvider _messageTypeToTopicProvider;
        private IHandlerTypeToChannelProvider _handlerTypeToChannelProvider;
        private string[] _defaultNsqdHttpEndpoints;
        private IBusStateChangedHandler _busStateChangedHandler;
        private ILogger _nsqLogger;
        private bool _preCreateTopicsAndChannels;
        private IMessageMutator _messageMutator;
        private IMessageTopicRouter _messageTopicRouter;

        private NsqBus _bus;

        internal BusConfiguration()
        {
        }

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
        /// <param name="defaultNsqLookupdHttpEndpoints">The default nsqlookupd HTTP endpoints; typically listening
        /// on port 4161.</param>
        /// <param name="defaultThreadsPerHandler">The default number of threads per message handler.</param>
        /// <param name="defaultConsumerNsqConfig">The default NSQ Consumer <see cref="Config"/> (optional).</param>
        /// <param name="busStateChangedHandler">Handle bus start and stop events (optional).</param>
        /// <param name="nsqLogger">The <see cref="ILogger"/> used by NsqSharp when communicating with nsqd/nsqlookupd.
        /// (default = <see cref="TraceLogger"/>).</param>
        /// <param name="preCreateTopicsAndChannels">Set to <c>true</c> to pre-create all registered topics and channels
        /// on the local nsqd instance listening on 127.0.0.1:4151; useful for self-contained clusters (default =
        /// <c>false</c>).</param>
        /// <param name="messageMutator">The message mutator used to modify a message before it's sent (optional).</param>
        /// <param name="messageTopicRouter">The message router used to specify custom message-to-topic routing logic; used
        /// to override <paramref name="messageTypeToTopicProvider"/> (optional).</param>
        public BusConfiguration(
            IObjectBuilder dependencyInjectionContainer,
            IMessageSerializer defaultMessageSerializer,
            IMessageAuditor messageAuditor,
            IMessageTypeToTopicProvider messageTypeToTopicProvider,
            IHandlerTypeToChannelProvider handlerTypeToChannelProvider,
            string[] defaultNsqLookupdHttpEndpoints,
            int defaultThreadsPerHandler,
            Config defaultConsumerNsqConfig = null,
            IBusStateChangedHandler busStateChangedHandler = null,
            ILogger nsqLogger = null,
            bool preCreateTopicsAndChannels = false,
            IMessageMutator messageMutator = null,
            IMessageTopicRouter messageTopicRouter = null
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
            if (defaultNsqLookupdHttpEndpoints == null)
                throw new ArgumentNullException("defaultNsqLookupdHttpEndpoints");
            if (defaultNsqLookupdHttpEndpoints.Length == 0)
                throw new ArgumentNullException("defaultNsqLookupdHttpEndpoints", "must contain elements");
            if (defaultThreadsPerHandler <= 0)
                throw new ArgumentOutOfRangeException("defaultThreadsPerHandler", "must be > 0");

            _topicChannelHandlers = new Dictionary<string, List<MessageHandlerMetadata>>();

            _messageTypeToTopicProvider = messageTypeToTopicProvider;
            _handlerTypeToChannelProvider = handlerTypeToChannelProvider;

            _dependencyInjectionContainer = dependencyInjectionContainer;
            _defaultMessageSerializer = defaultMessageSerializer;
            _messageAuditor = messageAuditor;
            _defaultNsqlookupdHttpEndpoints = defaultNsqLookupdHttpEndpoints;
            _defaultConsumerNsqConfig = defaultConsumerNsqConfig ?? new Config();
            _defaultThreadsPerHandler = defaultThreadsPerHandler;
            _defaultNsqdHttpEndpoints = new[] { "127.0.0.1:4151" };
            _busStateChangedHandler = busStateChangedHandler;
            _nsqLogger = nsqLogger ?? new TraceLogger();
            _preCreateTopicsAndChannels = preCreateTopicsAndChannels;
            _messageMutator = messageMutator;
            _messageTopicRouter = messageTopicRouter;

            var handlerTypes = _handlerTypeToChannelProvider.GetHandlerTypes();
            AddMessageHandlers(handlerTypes);
        }

      

        internal Dictionary<string, List<MessageHandlerMetadata>> TopicChannelHandlers
        {
            get { return _topicChannelHandlers; }
            set { _topicChannelHandlers = value; }
        }

        internal IObjectBuilder DependencyInjectionContainer
        {
            get { return _dependencyInjectionContainer; }
            set { _dependencyInjectionContainer = value; }
        }

        internal IMessageSerializer DefaultMessageSerializer
        {
            get { return _defaultMessageSerializer; }
            set { _defaultMessageSerializer = value; }
        }

        internal IMessageAuditor MessageAuditor
        {
            get { return _messageAuditor; }
            set { _messageAuditor = value; }
        }

        internal string[] DefaultNsqlookupdHttpEndpoints
        {
            get { return _defaultNsqlookupdHttpEndpoints; }
            set { _defaultNsqlookupdHttpEndpoints = value; }
        }

        internal Config DefaultConsumerNsqConfig
        {
            get { return _defaultConsumerNsqConfig; }
            set { _defaultConsumerNsqConfig = value; }
        }

        internal int DefaultThreadsPerHandler
        {
            get { return _defaultThreadsPerHandler; }
            set { _defaultThreadsPerHandler = value; }
        }

        internal IMessageTypeToTopicProvider MessageTypeToTopicProvider
        {
            get { return _messageTypeToTopicProvider; }
            set { _messageTypeToTopicProvider = value; }
        }

        internal IHandlerTypeToChannelProvider HandlerTypeToChannelProvider
        {
            get { return _handlerTypeToChannelProvider; }
            set { _handlerTypeToChannelProvider = value; }
        }

        internal string[] DefaultNsqdHttpEndpoints
        {
            get { return _defaultNsqdHttpEndpoints; }
            set { _defaultNsqdHttpEndpoints = value; }
        }

        internal IBusStateChangedHandler BusStateChangedHandler
        {
            get { return _busStateChangedHandler; }
            set { _busStateChangedHandler = value; }
        }

        internal ILogger NsqLogger
        {
            get { return _nsqLogger; }
            set { _nsqLogger = value; }
        }

        internal bool PreCreateTopicsAndChannels
        {
            get { return _preCreateTopicsAndChannels; }
            set { _preCreateTopicsAndChannels = value; }
        }

        internal IMessageMutator MessageMutator
        {
            get { return _messageMutator; }
            set { _messageMutator = value; }
        }

        internal IMessageTopicRouter MessageTopicRouter
        {
            get { return _messageTopicRouter; }
            set { _messageTopicRouter = value; }
        }

        internal NsqBus Bus
        {
            get { return _bus; }
            set { _bus = value; }
        }

        /// <summary>
        /// Add message handlers from the specified list of <paramref name="handlerTypes"/>.
        /// Uses defaults specified in the <see cref="BusConfiguration"/> constructor.
        /// </summary>
        /// <param name="handlerTypes">The message handler types to add. Throws if a type is an invalid message handler.</param>
        internal void AddMessageHandlers(IEnumerable<Type> handlerTypes)
        {
            if (handlerTypes == null)
                throw new ArgumentNullException("handlerTypes");

            foreach (var handlerType in handlerTypes)
            {
                List<Type> handlerMessageTypes = null;
                PopulateHandlerMessagesTypes(handlerType, ref handlerMessageTypes);

                if (handlerMessageTypes != null && handlerMessageTypes.Count != 0)
                {
                    if (handlerMessageTypes.Count > 1)
                    {
                        var handlerMessageTypesStrings =
                            handlerMessageTypes.Select(p => string.Format("IHandleMessages<{0}>", p.Name)).ToArray();

                        var handlesMessageTypes = string.Join(", ", handlerMessageTypesStrings);

                        var errorMessage = string.Format(
                            "Handler '{0}' implements multiple handlers: {1}. Register the IHandleMessages<T> interfaces " +
                            "themselves as handlers and register them with your DI container to resolve to the concrete " +
                            "handler type. This is to prevent the same channel name from unintentionally applying to " +
                            "multiple topics.",
                            handlerType, handlesMessageTypes
                        );

                        throw new HandlerConfigurationException(errorMessage);
                    }

                    Type messageType = handlerMessageTypes[0];
                    List<string> topics;

                    if (_messageTopicRouter == null)
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

                        topics = new List<string>();
                        topics.Add(topic);
                    }
                    else
                    {
                        topics = new List<string>(_messageTopicRouter.GetTopics(messageType));
                    }

                    string channel;
                    try
                    {
                        channel = _handlerTypeToChannelProvider.GetChannel(handlerType);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format(
                            "Channel for handler type '{0}' not registered.", handlerType.FullName), ex);
                    }

                    foreach (var topic in topics)
                    {
                        AddMessageHandler(handlerType, messageType, topic, channel);
                    }
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

            if (handlerType.IsAbstract && !handlerType.IsInterface)
                throw new ArgumentException("handlerType must be instantiable; cannot be an abstract class", "handlerType");

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

        private static void PopulateHandlerMessagesTypes(Type type, ref List<Type> messageTypes)
        {
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
            {
                if (messageTypes == null)
                    messageTypes = new List<Type>();
                var messageType = type.GetGenericArguments()[0];
                messageTypes.Add(messageType);
            }

            if (type.BaseType != typeof(object) && type.BaseType != null)
                PopulateHandlerMessagesTypes(type.BaseType, ref messageTypes);

            foreach (var interfaceType in type.GetInterfaces())
            {
                PopulateHandlerMessagesTypes(interfaceType, ref messageTypes);
            }
        }

        internal void StartBus()
        {
            if (_preCreateTopicsAndChannels)
            {
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
                                catch (Exception ex)
                                {
                                    _nsqLogger.Output(LogLevel.Error,
                                        string.Format("error creating topic/channel on {0} - {1}", localNsqdHttpAddress, ex));
                                }

                                wg.Done();
                            }, "BusConfiguration pre-create topics/channels");
                        }
                    }
                }

                wg.Wait();
            }

            if (_busStateChangedHandler != null)
                _busStateChangedHandler.OnBusStarting(this);

            _bus = new NsqBus(
                _topicChannelHandlers,
                _dependencyInjectionContainer,
                _messageTypeToTopicProvider,
                _defaultMessageSerializer,
                _defaultNsqdHttpEndpoints,
                _nsqLogger,
                _messageMutator,
                _messageTopicRouter
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
