using NsqSharp.Bus.Configuration;

public class ObjectClass : IObjectBuilder
{
    public T GetInstance<T>()
    {
        return default(T);
    }

    public object GetInstance(Type type)
    {
        throw new NotImplementedException();
    }

    public void Inject<T>(T instance) where T : class
    {
        throw new NotImplementedException();
    }
}