using System;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Logging;
using NsqSharp.Core;

namespace NsqSharp.Bus.Utils
{
    /// <summary>
    /// Fluent api for BusConfiguration
    /// </summary>
    /// <example>
    ///  <code>
    ///         new BusConfigurator().Configure(settings => {
    ///                         settings.UsingContainer(iObjectBuilder);
    ///                         settings.UsingSerializer(iMessageSerializer);
    ///                         settings.UsingMessageAuditor(iMessageAuditor);
    ///                         settings.UsingMessageTypeToTopicProvider(iMessageTypeToTopicProvider);
    ///                         settings.UsingHandlerTypeToChannelProvider(iHandlerTypeToChannelProvider);
    ///                         settings.UsingLookupdHttpEndpoints(new []{"127.0.0.1:4151"});
    ///                         settings.UsingTheseManyThreadsPerHandler(4);
    ///                     }).StartBus()
    ///                         .
    ///  </code>
    /// </example>
    public class BusConfigurator
    {
        private BusConfiguration _configuration;
        /// <summary>
        /// Starting point for configuration 
        /// </summary>
        /// <returns></returns>
        public BusConfiguration Configure(Action<BusConfigurator> configure)
        {
            return (_configuration = new BusConfiguration());
        }

        /// <summary>
        /// Configure IoC
        /// </summary>
        /// <param name="ioc"></param>
        /// <returns></returns>
        public BusConfiguration UsingContainer(IObjectBuilder ioc)
        {
            _configuration.DependencyInjectionContainer = ioc;
            return _configuration;
        }

        /// <summary>
        /// DependencyInjectionContainer must be specified using WithThisIoC before configuring serializer 
        /// </summary>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public BusConfiguration UsingSerializer(IMessageSerializer serializer)
        {
            if (_configuration.DependencyInjectionContainer == null)
                throw new ArgumentException("DependencyInjectionContainer must be specified using WithIoC before configuring serializer");
            _configuration.DefaultMessageSerializer = serializer;
            return _configuration;
        }

        /// <summary>
        /// Configure message auditor
        /// </summary>
        /// <param name="auditor"></param>
        /// <returns></returns>
        public BusConfiguration UsingMessageAuditor(IMessageAuditor auditor)
        {
            _configuration.MessageAuditor = auditor;
            return _configuration;
        }

        /// <summary>
        /// Configure MessageTypeToTopicProvider
        /// </summary>
        /// <param name="messageTypeToTopicProvider"></param>
        /// <returns></returns>
        public BusConfiguration UsingMessageTypeToTopicProvider(IMessageTypeToTopicProvider messageTypeToTopicProvider)
        {
            _configuration.MessageTypeToTopicProvider = messageTypeToTopicProvider;
            return _configuration;
        }


        /// <summary>
        /// Configure HandlerTypeToChannelProvider
        /// </summary>
        /// <param name="handlerTypeToChannelProvider"></param>
        /// <returns></returns>
        public BusConfiguration UsingHandlerTypeToChannelProvider(
            IHandlerTypeToChannelProvider handlerTypeToChannelProvider)
        {
            _configuration.HandlerTypeToChannelProvider = handlerTypeToChannelProvider;
            return _configuration;
        }


        /// <summary>
        /// Configure LookupdHttpEndpoints
        /// </summary>
        /// <param name="lookupdEndpoints"></param>
        /// <returns></returns>
        public BusConfiguration UsingLookupdHttpEndpoints(string[] lookupdEndpoints)
        {
            _configuration.DefaultNsqlookupdHttpEndpoints = lookupdEndpoints;
            return _configuration;
        }

        /// <summary>
        /// Configure handler thread count
        /// </summary>
        /// <param name="threads"></param>
        /// <returns></returns>
        public BusConfiguration UsingTheseManyThreadsPerHandler(int threads)
        {
            _configuration.DefaultThreadsPerHandler = threads;
            return _configuration;
        }

        /// <summary>
        /// Configure Nsq consumer config
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public BusConfiguration UsingConsumerNsqConfig(Config config)
        {
            _configuration.DefaultConsumerNsqConfig = config;
            return _configuration;
        }

        /// <summary>
        /// configure ConsumerNsqConfiguration
        /// </summary>
        /// <returns></returns>
        public BusConfiguration UsingDefaultConsumerNsqConfiguration()
        {
            _configuration.DefaultConsumerNsqConfig = new Config();
            return _configuration;
        }

        /// <summary>
        /// Configure bus state change handler
        /// </summary>
        /// <param name="busStateChangedHandler"></param>
        /// <returns></returns>
        public BusConfiguration UsingBusStateChangeHandler(IBusStateChangedHandler busStateChangedHandler)
        {
            _configuration.BusStateChangedHandler = busStateChangedHandler;
            return _configuration;
        }

        /// <summary>
        /// Configure logger
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public BusConfiguration UsingLogger(ILogger logger)
        {
            _configuration.NsqLogger = logger;
            return _configuration;
        }

        /// <summary>
        /// Configure Nsqd http endpoints
        /// </summary>
        /// <returns></returns>
        public BusConfiguration UsingDefaultNsqdHttpEndpoints()
        {
            _configuration.DefaultNsqdHttpEndpoints = new[] { "127.0.0.1:4151" };
            return _configuration;
        }

        /// <summary>
        /// Configure whether to pre create topics and channels
        /// </summary>
        /// <returns></returns>
        public BusConfiguration PreCreateTopicsAndChannels()
        {
            _configuration.PreCreateTopicsAndChannels = true;
            return _configuration;
        }

        /// <summary>
        /// Configure message mutator
        /// </summary>
        /// <param name="messageMutator"></param>
        /// <returns></returns>
        public BusConfiguration UsingMessageMutator(IMessageMutator messageMutator)
        {
            _configuration.MessageMutator = messageMutator;
            return _configuration;
        }

        /// <summary>
        /// Configure message topic router
        /// </summary>
        /// <param name="messageTopicRouter"></param>
        /// <returns></returns>
        public BusConfiguration UsingMessageTopicRouter(IMessageTopicRouter messageTopicRouter)
        {
            _configuration.MessageTopicRouter = messageTopicRouter;
            return _configuration;
        }

        /// <summary>
        /// End of configuration
        /// </summary>
        public void StartBus()
        {
            CheckConfiguration(_configuration);
            _configuration.AddMessageHandlers(_configuration.HandlerTypeToChannelProvider.GetHandlerTypes());
            BusService.Start(_configuration);
        }

        private void CheckConfiguration(BusConfiguration configuration)
        {
            if (configuration.DependencyInjectionContainer == null)
                throw new ArgumentException("DependencyInjectionContainer is not specified");
            if (configuration.DefaultMessageSerializer == null)
                throw new ArgumentException("DefaultMessageSerializeri is not specified");
            if (configuration.MessageAuditor == null)
                throw new ArgumentException("MessageAuditor is not specified");
            if (configuration.MessageTypeToTopicProvider == null)
                throw new ArgumentException("MessageTypeToTopicProvider is not specified");
            if (configuration.HandlerTypeToChannelProvider == null)
                throw new ArgumentException("HandlerTypeToChannelProvider is not specified");
            if (configuration.DefaultNsqlookupdHttpEndpoints == null || configuration.DefaultNsqlookupdHttpEndpoints.Length == 0)
                throw new ArgumentException("DefaultNsqLookupdHttpEndpoints is not specified");
            if (configuration.DefaultThreadsPerHandler <= 0)
                throw new ArgumentException("DefaultThreadsPerHandler must be > 0");

            if (configuration.DefaultConsumerNsqConfig == null)
                this.UsingDefaultConsumerNsqConfiguration();

        }

    }
}
