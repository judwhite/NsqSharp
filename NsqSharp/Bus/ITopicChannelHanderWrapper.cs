using System.Collections;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus;

public interface ITopicChannelHandlerWrapper {
    public Dictionary<string, List<MessageHandlerMetadata>> GetTopicChannelHandlers();
    public bool AddTopicChannelHandlers(string topic, List<MessageHandlerMetadata> messageHandlerMetadata);
}