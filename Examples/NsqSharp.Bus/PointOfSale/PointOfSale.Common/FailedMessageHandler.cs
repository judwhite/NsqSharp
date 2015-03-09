using System;
using NsqSharp;
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
            // TODO
            throw new NotImplementedException();
        }
    }
}
