namespace NsqSharp.Bus
{
    /// <summary>
    /// Configuration settings.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="channel"></param>
        /// <param name="lookupHttpAddresses"></param>
        void Subscribe<THandler, TMessage>(string topic, string channel, params string[] lookupHttpAddresses)
            where THandler : IHandleMessages<TMessage>;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="nsqdTcpAddresses"></param>
        void RegisterDestination<TMessage>(string topic, params string[] nsqdTcpAddresses);
    }
}
