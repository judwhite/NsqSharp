using PointOfSale.Common;
using PointOfSale.Handlers.InvoiceHandlers.Handlers;
using PointOfSale.Messages.Invoices;

namespace PointOfSale.Handlers.InvoiceHandlers
{
    public class ChannelProvider : ChannelProviderBase
    {
        public ChannelProvider()
        {
            Add<GetInvoiceDetailsHandler, GetInvoiceDetails>("get-invoice-details");
            Add<GetInvoicesHandler, GetInvoices>("get-invoices");
            Add<GetInvoiceSummaryHandler, GetInvoiceSummary>("get-invoice-summary");
        }
    }
}
