namespace NsqSharp.Bus.Configuration
{
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
