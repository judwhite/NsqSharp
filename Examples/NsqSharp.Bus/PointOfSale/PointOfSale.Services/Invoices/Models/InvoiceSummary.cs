namespace PointOfSale.Services.Invoices.Models
{
    public class InvoiceSummary
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
    }
}
