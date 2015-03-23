using System;

namespace PointOfSale.Messages.Audit
{
    public class MessageInformation
    {
        public Guid UniqueIdentifier { get; set; }
        public string Topic { get; set; }
        public string Channel { get; set; }
        public string HandlerType { get; set; }
        public string MessageType { get; set; }
        public string MessageId { get; set; }
        public int MessageAttempt { get; set; }
        public string MessageNsqdAddress { get; set; }
        public string MessageBody { get; set; }
        public DateTime MessageOriginalTimestamp { get; set; }
        public DateTime Started { get; set; }
        public DateTime? Finished { get; set; }
        public bool? Success { get; set; }
        public string FailedAction { get; set; }
        public string FailedReason { get; set; }
        public string FailedException { get; set; }
    }
}
