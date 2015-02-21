namespace NsqSharp.Bus.Configuration
{
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
}
