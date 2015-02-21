namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Object builder. See <see cref="IConfiguration.UsingContainer"/>.
    /// </summary>
    public interface IObjectBuilder
    {
        /// <summary>
        /// Creates or finds the registered instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The registered instance of type <typeparamref name="T"/>.</returns>
        T GetInstance<T>();
    }
}
