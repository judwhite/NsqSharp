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

        private Configure()
        {
        }

        /// <summary>
        /// Configures a new <see cref="IBus"/> using the specified <paramref name="assembliesToScan"/>.
        /// </summary>
        /// <param name="assembliesToScan">Assemblies to scan for <see cref="IHandleMessages&lt;T&gt;"/>,
        /// <see cref="ICommand"/> and <see cref="IEvent"/> implementations.</param>
        /// <returns>The <see cref="IConfiguration"/> for this bus.</returns>
        public static IConfiguration With(IEnumerable<Assembly> assembliesToScan)
        {
            return new NsqConfiguration(assembliesToScan);
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
}
