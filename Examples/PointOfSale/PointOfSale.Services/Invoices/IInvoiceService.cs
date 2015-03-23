using System.Collections.ObjectModel;
using PointOfSale.Services.Invoices.Models;

namespace PointOfSale.Services.Invoices
{
    public interface IInvoiceService
    {
        Collection<int> GetInvoiceIds();
        InvoiceSummary GetInvoiceSummary(int invoiceId);
        Collection<InvoiceDetail> GetInvoiceDetails(int invoiceId);
    }
}
