using System;
using System.Diagnostics;
using System.Security.Cryptography;
using NsqSharp.Bus;
using NsqSharp.Utils.Extensions;
using PointOfSale.Messages.Invoices.Events;
using PointOfSale.Services.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers.Handlers
{
    public class GetInvoiceDetailsHandler : IHandleMessages<InvoiceIdFoundEvent>
    {
        // TODO: make random failure an app.config setting
        private static readonly RNGCryptoServiceProvider _random = new RNGCryptoServiceProvider();

        private readonly IInvoiceService _invoiceService;
        
        public GetInvoiceDetailsHandler(IInvoiceService invoiceService)
        {
            if (invoiceService == null)
                throw new ArgumentNullException("invoiceService");

            _invoiceService = invoiceService;
        }

        public void Handle(InvoiceIdFoundEvent message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            //RandomFailure();

            var invoiceDetails = _invoiceService.GetInvoiceDetails(message.InvoiceId);

            foreach (var item in invoiceDetails)
            {
                Trace.WriteLine(string.Format("Invoice Details: Id: {0} ItemIdx: {1} ProductId: {2} Quantity: {3} Cost: {4:c}",
                    item.InvoiceId, item.ItemIndex, item.ProductId, item.Quantity, item.Cost));
            }
        }

        private void RandomFailure()
        {
            int n = _random.Intn(2);
            if (n == 0)
                throw new Exception("Random exception to test audit and backoff.");
        }
    }
}
