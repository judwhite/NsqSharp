using PointOfSale.Common;
using PointOfSale.Handlers.InvoiceHandlers.Handlers;
using PointOfSale.Messages.Invoices.Commands;
using PointOfSale.Messages.Invoices.Events;

namespace PointOfSale.Handlers.InvoiceHandlers
{
    public class ChannelProvider : ChannelProviderBase
    {
        public ChannelProvider()
        {
            Add<GetInvoicesHandler, GetInvoicesCommand>("get-invoices");
            Add<GetInvoiceDetailsHandler, InvoiceIdFoundEvent>("get-invoice-details");
            Add<GetInvoiceSummaryHandler, InvoiceIdFoundEvent>("get-invoice-summary");
        }
    }
}
