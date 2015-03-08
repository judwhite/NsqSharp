namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Implement <see cref="IBusStateChangedHandler"/> to act before and after the bus starts and stops.
    /// See <see cref="BusConfiguration"/>.
    /// </summary>
    public interface IBusStateChangedHandler
    {
        /// <summary>
        /// Occurs before the bus starts.
        /// If an exception is thrown in this method the bus will not start.
        /// </summary>
        /// <param name="config">The bus configuration.</param>
        void OnBusStarting(IBusConfiguration config);

        /// <summary>
        /// Occurs after the bus starts.
        /// </summary>
        /// <param name="config">The bus configuration.</param>
        /// <param name="bus">The bus.</param>
        void OnBusStarted(IBusConfiguration config, IBus bus);

        /// <summary>
        /// Occurs before the bus stops.
        /// If an exception is thrown in this method the bus will not stop.
        /// </summary>
        /// <param name="config">The bus configuration.</param>
        /// <param name="bus">The bus.</param>
        void OnBusStopping(IBusConfiguration config, IBus bus);

        /// <summary>
        /// Occurs after the bus stops.
        /// </summary>
        /// <param name="config">The bus configuration.</param>
        void OnBusStopped(IBusConfiguration config);
    }
}
