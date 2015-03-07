using System;
using NsqSharp.Bus;
using PointOfSale.Messages;
using PointOfSale.Services;

namespace PointOfSale.Handlers.Handlers
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
                throw new NotImplementedException();

            var customerIds = _customerService.GetCustomerIds();
            Console.WriteLine("Customer Count: {0}", customerIds.Count);
            foreach (var customerId in customerIds)
            {
                _bus.Send(new GetCustomerDetails { CustomerId = customerId });
            }
        }
    }
}
