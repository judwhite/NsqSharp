using System;
using System.Diagnostics;
using System.Linq;
using NsqSharp.Bus;
using PointOfSale.Messages.Invoices.Commands;
using PointOfSale.Messages.Invoices.Events;
using PointOfSale.Services.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers.Handlers
{
    public class GetInvoicesHandler : IHandleMessages<GetInvoicesCommand>
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

        public void Handle(GetInvoicesCommand message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var invoiceIds = _invoiceService.GetInvoiceIds();

            _bus.SendMulti(invoiceIds.Select(id => new InvoiceIdFoundEvent { InvoiceId = id }));

            Trace.WriteLine(string.Format("Invoice Count: {0}", invoiceIds.Count));
        }
    }
}
