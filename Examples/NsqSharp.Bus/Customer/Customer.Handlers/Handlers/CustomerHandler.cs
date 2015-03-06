using Customer.Messages;
using NsqSharp.Bus;

namespace Customer.Handlers.Handlers
{
    public class CustomerHandler : IHandleMessages<CustomerDetailsMessage>
    {
        public void Handle(CustomerDetailsMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
