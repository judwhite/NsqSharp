namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Implement this interface to configure an endpoint's handlers, message types, container, and serialization method.
    /// </summary>
    public interface IConfigureThisEndpoint
    {
        /// <summary>
        /// Initialization.
        /// </summary>
        void Init();
    }
}
