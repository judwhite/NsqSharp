using System;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using PointOfSale.Common;
using PointOfSale.Messages.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers
{
    class Program
    {
        static void Main()
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
                if (bus == null)
                    throw new ArgumentNullException("bus");

                bus.Send<GetInvoices>();
            }
        }
    }
}
