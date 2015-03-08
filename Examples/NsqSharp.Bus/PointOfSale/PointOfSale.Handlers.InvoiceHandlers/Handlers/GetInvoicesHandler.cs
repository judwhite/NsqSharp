using System;
using System.Linq;
using NsqSharp.Bus;
using PointOfSale.Messages.Invoices;
using PointOfSale.Services.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers.Handlers
{
    public class GetInvoicesHandler : IHandleMessages<GetInvoices>
    {
        private readonly IBus _bus;
        private readonly IInvoiceService _invoiceService;

        public GetInvoicesHandler(IBus bus, IInvoiceService invoiceService)
        {
            if (bus == null)
                throw new ArgumentNullException("bus");
            if (invoiceService == null)
                throw new ArgumentNullException("invoiceService");

            _bus = bus;
            _invoiceService = invoiceService;
        }

        public void Handle(GetInvoices message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var invoiceIds = _invoiceService.GetInvoiceIds();

            //_bus.SendMulti(invoiceIds.Select(id => new GetInvoiceDetails { InvoiceId = id }));
            //_bus.SendMulti(invoiceIds.Select(id => new GetInvoiceSummary { InvoiceId = id }));
            _bus.Send(new GetInvoiceSummary { InvoiceId = invoiceIds[0] });

            Console.WriteLine("Invoice Count: {0}", invoiceIds.Count);
        }
    }
}
