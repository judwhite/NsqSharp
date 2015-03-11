using System;
using System.Diagnostics;
using System.Linq;
using NsqSharp.Bus;
using PointOfSale.Messages.Customers.Commands;
using PointOfSale.Messages.Customers.Events;
using PointOfSale.Services.Customers;

namespace PointOfSale.Handlers.CustomerHandlers.Handlers
{
    public class GetCustomersHandler : IHandleMessages<GetCustomersCommand>
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

        public void Handle(GetCustomersCommand message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var customerIds = _customerService.GetCustomerIds();

            var getCustomersDetails = customerIds.Select(id => new CustomerIdFoundEvent { CustomerId = id });
            _bus.SendMulti(getCustomersDetails);

            Trace.WriteLine(string.Format("Customer Count: {0}", customerIds.Count));
        }
    }
}
