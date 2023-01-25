using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;

public class MessageMutatorClass : IMessageMutator
{
    public object MutateOutgoing(object message)
    {
        throw new NotImplementedException();
    }

    public object MutateIncoming(object message)
    {
        throw new NotImplementedException();
    }

    public T GetMutatedMessage<T>(IBus bus, T sentMessage)
    {
        throw new NotImplementedException();
    }
}