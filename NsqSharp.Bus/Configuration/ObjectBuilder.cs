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

    /// <summary>
    /// StructureMap object builder. See <see cref="IConfiguration.UsingContainer"/>.
    /// </summary>
    public class StructureMapObjectBuilder : IObjectBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StructureMapObjectBuilder"/> class.
        /// See <see cref="IConfiguration.UsingContainer"/>.
        /// </summary>
        /// <param name="objectFactoryContainer">StructureMap ObjectFactory.Container</param>
        public StructureMapObjectBuilder(object objectFactoryContainer)
        {
            // TODO: Use reflection to get GetInstance<> method
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Creates or finds the registered instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The registered instance of type <typeparamref name="T"/>.</returns>
        public T GetInstance<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}
