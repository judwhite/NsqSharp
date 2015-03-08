using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using PointOfSale.Common;
using PointOfSale.Messages.Customers;

namespace PointOfSale.Handlers.CustomerHandlers
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            PointOfSaleBus.Start(new ChannelProvider(), new BusStateChangedHandler());
        }

        public class BusStateChangedHandler : IBusStateChangedHandler
        {
            public void OnBusStarting(IBusConfiguration config) { }
            public void OnBusStopping(IBusConfiguration config, IBus bus) { }
            public void OnBusStopped(IBusConfiguration config) { }

            public void OnBusStarted(IBusConfiguration config, IBus bus)
            {
                bus.Send<GetCustomers>();
            }
        }
    }
}
