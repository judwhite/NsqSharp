using System;
using NsqSharp.Bus;
using PointOfSale.Messages.Customers.Events;
using PointOfSale.Services.Customers;

namespace PointOfSale.Handlers.CustomerHandlers.Handlers
{
    public class GetCustomerDetailsHandler : IHandleMessages<CustomerIdFoundEvent>
    {
        private readonly ICustomerService _customerService;

        public GetCustomerDetailsHandler(ICustomerService customerService)
        {
            if (customerService == null)
                throw new ArgumentNullException("customerService");

            _customerService = customerService;
        }

        public void Handle(CustomerIdFoundEvent message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var customer = _customerService.GetCustomer(message.CustomerId);

            Console.WriteLine("Customer: Id: {0} First: {1} Last: {2} Street: {3} City: {4}",
                customer.CustomerId, customer.FirstName, customer.LastName, customer.Street, customer.City);
        }
    }
}
