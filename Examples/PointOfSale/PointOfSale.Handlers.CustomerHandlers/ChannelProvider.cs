using PointOfSale.Common.Nsq;
using PointOfSale.Handlers.CustomerHandlers.Handlers;
using PointOfSale.Messages.Customers.Commands;
using PointOfSale.Messages.Customers.Events;

namespace PointOfSale.Handlers.CustomerHandlers
{
    public class ChannelProvider : ChannelProviderBase
    {
        public ChannelProvider()
        {
            Add<GetCustomersHandler, GetCustomersCommand>("get-customers");
            Add<GetCustomerDetailsHandler, CustomerIdFoundEvent>("get-customer-details");
        }
    }
}
