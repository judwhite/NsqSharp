using System;
using System.Diagnostics;
using NsqSharp.Bus.Logging;
using NsqSharp.Core;

namespace PointOfSale.Common
{
    public class FailedMessageHandler : IFailedMessageHandler
    {
        public void Handle(
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
            string logEntry = string.Format("id: {0} action:{1} reason:{2} topic:{3} channel:{4} msg:{5} ex:{6}",
                 message.IdHexString, action, reason, topic, channel, deserializedMessageBody, exception);
            
            if (action == FailedMessageQueueAction.Requeue)
            {
                Trace.TraceWarning(logEntry);
            }
            else
            {
                Trace.TraceError(logEntry);
            }
        }
    }
}
