namespace Customer.Messages
{
    public class InvoiceDetailsMessage
    {
        public int InvoiceId { get; set; }
        public int ItemNumber { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
    }
}
