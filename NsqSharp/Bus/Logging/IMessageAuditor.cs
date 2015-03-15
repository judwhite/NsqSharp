using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus.Logging
{
    /// <summary>
    /// Implement <see cref="IMessageAuditor" /> to handle auditing of started, succeeded, and failed messages.
    /// <seealso cref="BusConfiguration"/>.
    /// </summary>
    public interface IMessageAuditor
    {
        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="info">Message information including the topic, channel, and raw message.</param>
        void OnReceived(IBus bus, IMessageInformation info);

        /// <summary>
        /// Occurs when a message handler succeeds.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="info">Message information including the topic, channel, and raw message.</param>
        void OnSucceeded(IBus bus, IMessageInformation info);

        /// <summary>
        /// Occurs when a message handler fails.
        /// </summary>
        /// <param name="bus">The bus.</param>
        /// <param name="failedInfo">Message information including the topic, channel, and raw message.</param>
        void OnFailed(IBus bus, IFailedMessageInformation failedInfo);
    }
}
