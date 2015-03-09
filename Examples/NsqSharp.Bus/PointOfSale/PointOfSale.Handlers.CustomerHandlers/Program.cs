using System.Reflection;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using PointOfSale.Common;
using PointOfSale.Messages.Customers.Commands;

namespace PointOfSale.Handlers.CustomerHandlers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PointOfSaleBus.Start(new ChannelProvider(), new[] { typeof(Program).Assembly }, new BusStateChangedHandler());
        }

        public class BusStateChangedHandler : IBusStateChangedHandler
        {
            public void OnBusStarting(IBusConfiguration config) { }
            public void OnBusStopping(IBusConfiguration config, IBus bus) { }
            public void OnBusStopped(IBusConfiguration config) { }

            public void OnBusStarted(IBusConfiguration config, IBus bus)
            {
                if (config.IsConsoleMode && Assembly.GetEntryAssembly() == typeof(Program).Assembly)
                {
                    bus.Send<GetCustomersCommand>();
                }
            }
        }
    }
}
