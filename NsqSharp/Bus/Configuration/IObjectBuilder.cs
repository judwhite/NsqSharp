using System;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Object builder interface.
    /// </summary>
    public interface IObjectBuilder
    {
        /// <summary>
        /// Creates or finds the registered instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The registered instance of type <typeparamref name="T"/>.</returns>
        T GetInstance<T>();

        /// <summary>
        /// Creates or finds the registered instance of the specifid <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns>The registered instance of the specifid <paramref name="type"/>.</returns>
        object GetInstance(Type type);

        /// <summary>
        /// Injects an <paramref name="instance"/> of type <typeparamref name="T"/> into the container.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="instance">The instance to inject.</param>
        void Inject<T>(T instance)
            where T : class;
    }
}
