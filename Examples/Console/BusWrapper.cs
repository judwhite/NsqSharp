using NsqSharp;
using NsqSharp.Bus;

public class BusWrapper : IBus
{
    public IMessage CurrentThreadMessage => throw new NotImplementedException();

    public ICurrentMessageInformation GetCurrentThreadMessageInformation()
    {
        throw new NotImplementedException();
    }

    public void Send<T>(T message)
    {
        throw new NotImplementedException();
    }

    public void Send<T>()
    {
        throw new NotImplementedException();
    }

    public void Send<T>(Action<T> messageConstructor)
    {
        throw new NotImplementedException();
    }

    public void SendMulti<T>(IEnumerable<T> messages)
    {
        throw new NotImplementedException();
    }
}