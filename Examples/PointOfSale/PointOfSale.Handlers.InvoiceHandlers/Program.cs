using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.WindowService;
using PointOfSale.Common.Nsq;
using PointOfSale.Messages.Invoices.Commands;

namespace PointOfSale.Handlers.InvoiceHandlers
{
    public class Program
    {
        // This project demos two handlers listening to the same topic on different channels.

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
#if DEBUG
                if ((config as IWindowsBusConfiguration).IsConsoleMode)
                {
                    bus.Send<GetInvoicesCommand>();
                }
#endif
            }
        }
    }
}
