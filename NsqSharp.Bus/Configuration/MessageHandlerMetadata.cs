using System;
using NsqSharp.Bus.Logging;

namespace NsqSharp.Bus.Configuration
{
    internal class MessageHandlerMetadata
    {
        public string Topic { get; set; }
        public string Channel { get; set; }
        public Type HandlerType { get; set; }
        public Type MessageType { get; set; }
        public string[] NsqLookupdHttpAddresses { get; set; }
        public IMessageSerializer Serializer { get; set; }
        public IFailedMessageHandler FailedMessageHandler { get; set; }
        public Config Config { get; set; }
        public int InstanceCount { get; set; }

        public Consumer Consumer { get; set; }
    }
}
