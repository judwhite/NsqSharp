using PointOfSale.Common;
using PointOfSale.Handlers.CustomerHandlers.Handlers;
using PointOfSale.Messages.Customers;

namespace PointOfSale.Handlers.CustomerHandlers
{
    public class ChannelProvider : ChannelProviderBase
    {
        public ChannelProvider()
        {
            Add<GetCustomerDetailsHandler, GetCustomerDetails>("get-customer-details");
            Add<GetCustomersHandler, GetCustomers>("get-customers");
        }
    }
}
