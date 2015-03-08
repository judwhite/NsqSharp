using System;
using NsqSharp.Bus;
using PointOfSale.Messages.Invoices;
using PointOfSale.Services.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers.Handlers
{
    public class GetInvoiceDetailsHandler : IHandleMessages<GetInvoiceDetails>
    {
        private readonly IInvoiceService _invoiceService;

        public GetInvoiceDetailsHandler(IInvoiceService invoiceService)
        {
            if (invoiceService == null)
                throw new ArgumentNullException("invoiceService");

            _invoiceService = invoiceService;
        }

        public void Handle(GetInvoiceDetails message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var invoiceDetails = _invoiceService.GetInvoiceDetails(message.InvoiceId);

            foreach (var item in invoiceDetails)
            {
                Console.WriteLine("Invoice Details: Id: {0} ItemIdx: {1} ProductId: {2} Quantity: {3} Cost: {4:c}",
                    item.InvoiceId, item.ItemIndex, item.ProductId, item.Quantity, item.Cost);
            }
        }
    }
}
