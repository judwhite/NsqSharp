using NsqSharp.Bus.Configuration;

public class MessageSerializerClass : IMessageSerializer
{

    public byte[] Serialize(object value)
    {
        throw new NotImplementedException();
    }

    public object Deserialize(Type type, byte[] value)
    {
        throw new NotImplementedException();
    }
}