using System;

namespace NsqSharp.Bus.Configuration
{
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
}
