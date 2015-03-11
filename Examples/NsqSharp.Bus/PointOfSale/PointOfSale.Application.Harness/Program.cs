using System;
using System.Diagnostics;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using PointOfSale.Common;
using PointOfSale.Messages.Customers.Commands;
using PointOfSale.Messages.Invoices.Commands;
using PointOfSale.Messages.Products.Commands;

namespace PointOfSale.Application.Harness
{
    class Program
    {
        static void Main()
        {
            var channelProviders = new ChannelProviderBase[]
                {
                    new Handlers.CustomerHandlers.ChannelProvider(),
                    new Handlers.InvoiceHandlers.ChannelProvider(),
                    new Handlers.ProductHandlers.ChannelProvider()
                };

            PointOfSaleBus.Start(
                channelProvider: new CompositeChannelProvider(channelProviders),
                busStateChangedHandler: new BusStateChangedHandler()
            );
        }

        public class BusStateChangedHandler : IBusStateChangedHandler
        {
            public void OnBusStarting(IBusConfiguration config) { }
            public void OnBusStopping(IBusConfiguration config, IBus bus) { }
            public void OnBusStopped(IBusConfiguration config) { }

            public void OnBusStarted(IBusConfiguration config, IBus bus)
            {
                if (config.IsConsoleMode)
                {
                    Trace.Write("Press enter to begin . . . ");
                    while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                    Trace.WriteLine(string.Empty);

                    bus.Send<GetCustomersCommand>();
                    bus.Send<GetInvoicesCommand>();
                    bus.Send<GetProductsCommand>();
                }
            }
        }
    }
}
