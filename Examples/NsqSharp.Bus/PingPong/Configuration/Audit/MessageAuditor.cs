using System;
using NsqSharp.Bus;
using NsqSharp.Bus.Logging;

namespace PingPong.Configuration.Audit
{
    public class MessageAuditor : IMessageAuditor
    {
        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo)
        {
            string action;
            if (failedInfo.FailedAction == FailedMessageQueueAction.Requeue)
                action = "Requeueing...";
            else
                action = "Permanent failure.";

            Console.WriteLine("[{0}] {1} Message ID {2} on topic {3} channel {4} failed - {5}", DateTime.Now,
                action, failedInfo.Message.Id, failedInfo.Topic, failedInfo.Channel, failedInfo.FailedException);
        }

        public void OnReceived(IBus bus, IMessageInformation info) { }
        public void OnSucceeded(IBus bus, IMessageInformation info) { }
    }
}
