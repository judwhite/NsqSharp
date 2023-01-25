using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;

public class MessageTopicRouterClass : IMessageTopicRouter
{
    public string GetMessageTopic<T>(IBus bus, string originalTopic, T sentMessage)
    {
        return "msg topic";
    }

    public string GetTopic(Type messageType)
    {
        return "topic";
    }

    public string[] GetTopics(Type messageType)
    {
        return new string[] {"topc1", "topic2"};
    }
}