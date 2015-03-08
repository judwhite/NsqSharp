namespace PointOfSale.Services.Invoices.Models
{
    public class InvoiceDetail
    {
        public int InvoiceId { get; set; }
        public int ItemIndex { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
    }
}
