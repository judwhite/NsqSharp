namespace Customer.Messages
{
    public class ProductDetailsMessage
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
