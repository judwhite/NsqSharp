using System;
using NsqSharp.Bus.Logging;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>
    /// Convenience class for writing exceptions to the console. In Production a more robust auditing implementation
    /// should be used. See the PointOfSale example for an implementation which uses NLog and SQL Server.
    /// </summary>
    public class ConsoleMessageAuditor : IMessageAuditor
    {
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="info">Message information including the topic, channel, and raw message.</param>
        public void OnReceived(IBus bus, IMessageInformation info) { }

        /// <summary>
        /// Occurs when a message handler succeeds.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="info">Message information including the topic, channel, and raw message.</param>
        public void OnSucceeded(IBus bus, IMessageInformation info) { }

        /// <summary>
        /// Occurs when a message handler fails.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="failedInfo">Message information including the topic, channel, and raw message.</param>
        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo)
        {
            Console.WriteLine(failedInfo.Exception);
        }
    }
}
