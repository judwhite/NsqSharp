using System;
using System.Collections.Generic;
using System.Reflection;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Configures a new bus.
    /// </summary>
    public class Configure
    {
        private static readonly Configure _configure = new Configure();
        private static readonly IConfigureSerialization _configureSerialization = new ConfigureSerialization();
        private static NsqConfiguration _nsqConfiguration;
        private static bool _useInProcessBus;
        private static bool _isBusCreated;

        private Configure()
        {
            Configurer = new ConfigureComponents();
        }

        /// <summary>
        /// Starts the bus.
        /// </summary>
        /// <typeparam name="TEndpointConfig">The endpoint config.</typeparam>
        public static IBus StartBus<TEndpointConfig>()
            where TEndpointConfig : IConfigureThisEndpoint, new()
        {
            var endpointConfig = new TEndpointConfig();
            endpointConfig.Init();

            return _nsqConfiguration.StartBus();
        }

        /// <summary>
        /// Call to use an in-process bus which doesn't call NSQ.
        /// </summary>
        public static void UseInProcessBus()
        {
            if (_isBusCreated && !_useInProcessBus)
                throw new Exception("Call UseInProcessBus() before calling Configure.With");

            _useInProcessBus = true;
        }

        internal static bool IsInProcessBus
        {
            get { return _useInProcessBus; }
        }

        /// <summary>
        /// Gets the <see cref="Configure"/> instance.
        /// </summary>
        public static Configure Instance
        {
            get { return _configure; }
        }

        /// <summary>
        /// Configures a new <see cref="IBus"/> using the specified <paramref name="assembliesToScan"/>.
        /// </summary>
        /// <param name="assembliesToScan">Assemblies to scan for <see cref="IHandleMessages&lt;T&gt;"/>,
        /// <see cref="ICommand"/> and <see cref="IEvent"/> implementations.</param>
        /// <returns>The <see cref="IConfiguration"/> for this bus.</returns>
        public static IConfiguration With(IEnumerable<Assembly> assembliesToScan)
        {
            _isBusCreated = true;

            if (_nsqConfiguration == null)
                _nsqConfiguration = new NsqConfiguration(assembliesToScan);
            else
                _nsqConfiguration.ScanAssemblies(assembliesToScan);

            return _nsqConfiguration;
        }

        /// <summary>
        /// Set the default serialization method.
        /// </summary>
        public static IConfigureSerialization Serialization
        {
            get { return _configureSerialization; }
        }

        /// <summary>
        /// Gets the component configurer.
        /// </summary>
        public IConfigureComponents Configurer { get; private set; }

        /// <summary>
        /// Gets the component builder.
        /// </summary>
        public IObjectBuilder Builder { get; internal set; }
    }
}
