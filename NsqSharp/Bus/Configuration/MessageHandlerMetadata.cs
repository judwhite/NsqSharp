using System;
using NsqSharp.Bus.Logging;

namespace NsqSharp.Bus.Configuration
{
    public class MessageHandlerMetadata
    {
        public string Topic { get; set; }
        public string Channel { get; set; }
        public Type HandlerType { get; set; }
        public Type MessageType { get; set; }
        public string[] NsqLookupdHttpAddresses { get; set; }
        public IMessageSerializer Serializer { get; set; }
        public IMessageAuditor MessageAuditor { get; set; }
        public IPleaseWorkConfig Config { get; set; }
        public int InstanceCount { get; set; }

        public Consumer Consumer { get; set; }
    }
}
