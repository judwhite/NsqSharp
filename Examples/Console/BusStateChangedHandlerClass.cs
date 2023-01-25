using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;

internal class BusStateChangedHandlerClass : IBusStateChangedHandler
{
    public void OnBusStarted(IBusConfiguration config, IBus bus)
    {
        throw new NotImplementedException();
    }

    public void OnBusStarting(IBusConfiguration config)
    {
        throw new NotImplementedException();
    }

    public void OnBusStopped(IBusConfiguration config)
    {
        throw new NotImplementedException();
    }

    public void OnBusStopping(IBusConfiguration config, IBus bus)
    {
        throw new NotImplementedException();
    }
}