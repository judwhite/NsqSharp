using NsqSharp.Bus;
using NsqSharp.Bus.Logging;

public class MessageAuditorClass : IMessageAuditor
{
    public void AuditMessage<T>(T message, string topic, string channel, string messageId, string messageBody)
    {
        throw new NotImplementedException();
    }

    public void OnFailed(IBus bus, IFailedMessageInformation failedInfo)
    {
        throw new NotImplementedException();
    }

    public void OnReceived(IBus bus, IMessageInformation info)
    {
        throw new NotImplementedException();
    }

    public void OnSucceeded(IBus bus, IMessageInformation info)
    {
        throw new NotImplementedException();
    }
}