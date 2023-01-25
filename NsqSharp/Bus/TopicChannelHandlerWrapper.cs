using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus;
public class TopicChannelHandlerWrapper : ITopicChannelHandlerWrapper
{
    private Dictionary<string, List<MessageHandlerMetadata>> _topicChannelHandlers;

    public bool AddTopicChannelHandlers(string topic, List<MessageHandlerMetadata> messageHandlerMetadata)
    {
        return true;
    }

    public Dictionary<string, List<MessageHandlerMetadata>> GetTopicChannelHandlers()
    {
        return new Dictionary<string, List<MessageHandlerMetadata>>() {
            { "topic", new List<MessageHandlerMetadata>() {
                new MessageHandlerMetadata() {
                    HandlerType = typeof(MessageHandlerClass),
                    MessageType = typeof(MessageClass),
                }
            } }
        };
    }

    public class MessageHandlerClass { }
    public class MessageClass { }
}