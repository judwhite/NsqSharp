using System;
using System.Linq;
using NsqSharp.Bus;
using PointOfSale.Messages;
using PointOfSale.Messages.Customers;
using PointOfSale.Services;
using PointOfSale.Services.Customers;

namespace PointOfSale.Handlers.CustomerHandlers.Handlers
{
    public class GetCustomersHandler : IHandleMessages<GetCustomers>
    {
        private readonly IBus _bus;
        private readonly ICustomerService _customerService;

        public GetCustomersHandler(IBus bus, ICustomerService customerService)
        {
            if (bus == null)
                throw new ArgumentNullException("bus");
            if (customerService == null)
                throw new ArgumentNullException("customerService");

            _bus = bus;
            _customerService = customerService;
        }

        public void Handle(GetCustomers message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var customerIds = _customerService.GetCustomerIds();

            var getCustomersDetails = customerIds.Select(id => new GetCustomerDetails { CustomerId = id });
            _bus.SendMulti(getCustomersDetails);

            Console.WriteLine("Customer Count: {0}", customerIds.Count);
        }
    }
}
