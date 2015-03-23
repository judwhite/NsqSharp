using System.Diagnostics;
using System.Text;
using NsqSharp.Bus;
using NsqSharp.Bus.Logging;
using PointOfSale.Messages.Audit;

namespace PointOfSale.Common.Nsq
{
    public class MessageAuditor : IMessageAuditor
    {
        public void OnReceived(IBus bus, IMessageInformation info)
        {
            if (info.MessageType != typeof(MessageInformation))
            {
                bus.Send(Convert(info));
            }
        }

        public void OnSucceeded(IBus bus, IMessageInformation info)
        {
            if (info.MessageType != typeof(MessageInformation))
            {
                bus.Send(Convert(info));
            }
        }

        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo)
        {
            if (failedInfo.MessageType != typeof(MessageInformation))
            {
                bus.Send(Convert(failedInfo));
            }
            else
            {
                // failed audit

                string logEntry = string.Format("id: {0} action:{1} reason:{2} topic:{3} channel:{4} msg:{5} ex:{6}",
                     failedInfo.Message.Id, failedInfo.FailedAction, failedInfo.FailedReason, failedInfo.Topic, failedInfo.Channel,
                     Encoding.UTF8.GetString(failedInfo.Message.Body), failedInfo.Exception);

                if (failedInfo.FailedAction == FailedMessageQueueAction.Requeue)
                {
                    Trace.TraceWarning(logEntry);
                }
                else
                {
                    Trace.TraceError(logEntry);
                }
            }
        }

        private static MessageInformation Convert(IMessageInformation info)
        {
            return new MessageInformation
            {
                UniqueIdentifier = info.UniqueIdentifier,
                Topic = info.Topic,
                Channel = info.Channel,
                HandlerType = info.HandlerType.FullName,
                MessageType = info.MessageType.FullName,
                MessageId = info.Message.Id,
                MessageAttempt = info.Message.Attempts,
                MessageNsqdAddress = info.Message.NsqdAddress,
                MessageBody = TryGetString(info.Message.Body),
                MessageOriginalTimestamp = info.Message.Timestamp,
                Started = info.Started,
                Finished = info.Finished,
                Success = (info.Finished == null ? null : (bool?)true)
            };
        }

        private static MessageInformation Convert(IFailedMessageInformation info)
        {
            return new MessageInformation
            {
                UniqueIdentifier = info.UniqueIdentifier,
                Topic = info.Topic,
                Channel = info.Channel,
                HandlerType = info.HandlerType.FullName,
                MessageType = info.MessageType.FullName,
                MessageId = info.Message.Id,
                MessageAttempt = info.Message.Attempts,
                MessageNsqdAddress = info.Message.NsqdAddress,
                MessageBody = TryGetString(info.Message.Body),
                MessageOriginalTimestamp = info.Message.Timestamp,
                Started = info.Started,
                Finished = info.Finished,
                Success = false,
                FailedAction = info.FailedAction.ToString(),
                FailedReason = info.FailedReason.ToString(),
                FailedException = info.Exception != null ? info.Exception.ToString() : null
            };
        }

        private static string TryGetString(byte[] data)
        {
            try
            {
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
            }

            return null;
        }
    }
}
