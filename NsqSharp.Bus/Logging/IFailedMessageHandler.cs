using System;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus.Logging
{
    /// <summary>
    /// Implement <see cref="IFailedMessageHandler" /> to handle failed messages. See <see cref="BusConfiguration"/>.
    /// </summary>
    public interface IFailedMessageHandler
    {
        /// <summary>
        /// Handle a failed message.
        /// </summary>
        /// <param name="action">The queue action taken for the failed message.</param>
        /// <param name="reason">The category of mesage failure.</param>
        /// <param name="topic">The topic the message was delivered on.</param>
        /// <param name="channel">The channel the message was delivered on.</param>
        /// <param name="handlerType">The handler .NET type.</param>
        /// <param name="messageType">The message .NET type.</param>
        /// <param name="message">The message.</param>
        /// <param name="deserializedMessageBody">The deserialized message body (can be <c>null</c>).</param>
        /// <param name="exception">The exception (can be <c>null</c>).</param>
        void Handle(
            FailedMessageQueueAction action,
            FailedMessageReason reason,
            string topic,
            string channel,
            Type handlerType,
            Type messageType,
            Message message,
            object deserializedMessageBody,
            Exception exception
        );
    }
}
