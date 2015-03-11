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
            Trace.WriteLine(string.Format("[FAIL] action:{0} reason:{1} topic:{2} channel:{3} msg:{4} ex:{5}",
                action, reason, topic, channel, deserializedMessageBody, exception));
        }
    }
}
