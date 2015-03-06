namespace Customer.Messages
{
    public class InvoiceSummaryMessage
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
    }
}
