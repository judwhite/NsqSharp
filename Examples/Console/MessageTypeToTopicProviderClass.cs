using NsqSharp.Bus.Configuration.Providers;

public class MessageTypeToTopicProviderClass : IMessageTypeToTopicProvider
{
    public string GetTopic(Type messageType)
    {
        throw new NotImplementedException();
    }
}