using NsqSharp.Bus.Logging;

namespace NsqSharp.Bus.Tests.Fakes
{
    public class MessageAuditorStub : IMessageAuditor
    {
        public void OnReceived(IBus bus, IMessageInformation info) { }
        public void OnSucceeded(IBus bus, IMessageInformation info) { }
        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo) { }
    }
}
