using System;

namespace NsqSharp.Bus.Logging
{
    internal static class IFailedMessageHandlerExtensions
    {
        public static void TryHandle(
            this IFailedMessageHandler failedMessageHandler,
            FailedMessageQueueAction action,
            FailedMessageReason reason,
            string topic,
            string channel,
            Type handlerType,
            Type messageType,
            Message message,
            object deserializedMessageBody,
            Exception exception
        )
        {
            try
            {
                failedMessageHandler.Handle(action, reason, topic, channel,
                    handlerType, messageType, message, deserializedMessageBody, exception);
            }
            catch (Exception)
            {
                // TODO: Log?
            }
        }
    }
}
