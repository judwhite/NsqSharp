using NsqSharp.Bus.Configuration;

public class NsqdPublisherClass : INsqdPublisher
{
    public void MultiPublish(string topic, IEnumerable<byte[]> messages)
    {
        throw new NotImplementedException();
    }

    public void Publish(string topic, byte[] message)
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }
}