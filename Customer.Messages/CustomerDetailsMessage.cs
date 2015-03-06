namespace Customer.Messages
{
    public class CustomerDetailsMessage
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
    }
}
