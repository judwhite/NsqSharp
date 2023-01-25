using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;

public class ThisTopicChannel : ITopicChannelHandlerWrapper
{
    public bool AddTopicChannelHandlers(string topic, List<MessageHandlerMetadata> messageHandlerMetadata)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, List<MessageHandlerMetadata>> GetTopicChannelHandlers()
    {
        throw new NotImplementedException();
    }
}