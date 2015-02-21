using System;

namespace NsqSharp.Bus
{
    /// <summary>
    /// Configures a new bus.
    /// </summary>
    public class Configure
    {
        private static readonly Configure _configure = new Configure();

        private Configure()
        {
        }

        /// <summary>
        /// Configures a new <see cref="IBus"/> using the specified <paramref name="busType"/>.
        /// </summary>
        /// <param name="busType">The <see cref="BusType"/>.</param>
        /// <returns>The <see cref="IConfiguration"/> for this bus.</returns>
        public static IConfiguration With(BusType busType)
        {
            return new Configuration(busType);
        }

        /// <summary>
        /// Gets the <see cref="Configure"/> instance.
        /// </summary>
        public static Configure Instance 
        {
            get { return _configure; }
        }

        /// <summary>
        /// Gets the component configurer.
        /// </summary>
        public IConfigureComponents Configurer { get; set; }

        
        /// <summary>
        /// Gets the component builder.
        /// </summary>
        public IBuilder Builder { get; set; }
    }

    /// <summary>
    /// IConfigureComponents
    /// </summary>
    public interface IConfigureComponents
    {
        /// <summary>
        /// Configures a component with the container.
        /// </summary>
        /// <typeparam name="T">The Type of the component.</typeparam>
        /// <param name="componentBuilder">The function used to build the component.</param>
        /// <param name="dependencyLifecycle">The <see cref="DependencyLifecycle"/> of the component.</param>
        void ConfigureComponent<T>(Func<T> componentBuilder, DependencyLifecycle dependencyLifecycle);

        /// <summary>
        /// Configures a component with the container.
        /// </summary>
        /// <typeparam name="T">The Type of the component.</typeparam>
        /// <param name="dependencyLifecycle">The <see cref="DependencyLifecycle"/> of the component.</param>
        void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle);
    }

    /// <summary>
    /// Specifies dependency lifecycle
    /// </summary>
    public enum DependencyLifecycle
    {
        /// <summary>
        /// Create a new instance every time the object is requested.
        /// </summary>
        InstancePerUnitOfWork,
        /// <summary>
        /// Create a single instance throughout the lifetime of this process.
        /// </summary>
        SingleInstance
    }

    /// <summary>
    /// Generic object builder using assigned container.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// Builds an object.
        /// </summary>
        /// <param name="type">The type of the object.</param>
        /// <returns>The constructred object with dependencies resolved.</returns>
        object Build(Type type);

        /// <summary>
        /// Builds an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The constructred object with dependencies resolved.</returns>
        T Build<T>();
    }

    /// <summary>
    /// Implement this interface to execute an Init method before the bus starts.
    /// </summary>
    public interface INeedInitialization
    {
        /// <summary>
        /// Initialization.
        /// </summary>
        void Init();
    }

    /// <summary>
    /// Implement this interface to execute methods when the bus starts and stops.
    /// </summary>
    public interface IWantToRunWhenBusStartsAndStops
    {
        /// <summary>
        /// Occurs when the bus starts.
        /// </summary>
        void Start();

        /// <summary>
        /// Occurs when the bus stops.
        /// </summary>
        void Stop();
    }
}
