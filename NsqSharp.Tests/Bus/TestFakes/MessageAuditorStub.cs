using NsqSharp.Bus;
using NsqSharp.Bus.Logging;

namespace NsqSharp.Tests.Bus.TestFakes
{
    public class MessageAuditorStub : IMessageAuditor
    {
        public void OnReceived(IBus bus, IMessageInformation info) { }
        public void OnSucceeded(IBus bus, IMessageInformation info) { }
        public void OnFailed(IBus bus, IFailedMessageInformation failedInfo) { }
    }
}
