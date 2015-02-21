namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Configuration settings.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Register an IoC container with the bus.
        /// </summary>
        /// <param name="objectBuilder">The <see cref="IObjectBuilder"/>. See <see cref="StructureMapObjectBuilder"/>
        /// for a built in implementation.</param>
        IConfiguration UsingContainer(IObjectBuilder objectBuilder);

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="channel"></param>
        /// <param name="lookupHttpAddresses"></param>
        IConfiguration Subscribe<THandler, TMessage>(string topic, string channel, params string[] lookupHttpAddresses)
            where THandler : IHandleMessages<TMessage>;

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="nsqdTcpAddresses"></param>
        IConfiguration RegisterDestination<TMessage>(string topic, params string[] nsqdTcpAddresses);
    }
}
