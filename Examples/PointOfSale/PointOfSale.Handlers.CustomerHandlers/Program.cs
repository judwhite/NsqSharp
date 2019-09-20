using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.WindowService;
using PointOfSale.Common.Nsq;
using PointOfSale.Messages.Customers.Commands;

namespace PointOfSale.Handlers.CustomerHandlers
{
    public class Program
    {
        public static void Main()
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
#if DEBUG
                if (((IWindowsBusConfiguration)config).IsConsoleMode)
                {
                    bus.Send<GetCustomersCommand>();
                }
#endif
            }
        }
    }
}
