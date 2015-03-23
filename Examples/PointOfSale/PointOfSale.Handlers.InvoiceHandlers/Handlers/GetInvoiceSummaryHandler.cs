using System;
using System.Diagnostics;
using NsqSharp.Bus;
using PointOfSale.Messages.Invoices.Events;
using PointOfSale.Services.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers.Handlers
{
    public class GetInvoiceSummaryHandler : IHandleMessages<InvoiceIdFoundEvent>
    {
        private readonly IInvoiceService _invoiceService;

        public GetInvoiceSummaryHandler(IInvoiceService invoiceService)
        {
            if (invoiceService == null)
                throw new ArgumentNullException("invoiceService");

            _invoiceService = invoiceService;
        }

        public void Handle(InvoiceIdFoundEvent message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var summary = _invoiceService.GetInvoiceSummary(message.InvoiceId);

            Trace.WriteLine(string.Format("Invoice Summary: InvoiceId: {0} CustomerId: {1} Total: {2:c}",
                summary.InvoiceId, summary.CustomerId, summary.Total));
        }
    }
}
