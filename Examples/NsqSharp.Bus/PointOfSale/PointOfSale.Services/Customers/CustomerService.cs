using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using PointOfSale.Common.Config;
using PointOfSale.Common.Utils;
using PointOfSale.Services.Customers.Models;

namespace PointOfSale.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly string _endpoint;
        private readonly IRestClient _restClient;

        public CustomerService(IServiceEndpoints serviceEndpoints, IRestClient restClient)
        {
            if (serviceEndpoints == null)
                throw new ArgumentNullException("serviceEndpoints");
            if (restClient == null)
                throw new ArgumentNullException("restClient");

            _endpoint = serviceEndpoints.CustomerEndpoint;
            _restClient = restClient;
        }

        public Collection<int> GetCustomerIds()
        {
            string response = _restClient.Get(_endpoint);

            var customerIds = XDocument.Parse(response).Root.Elements("CUSTOMER").Select(p => (int)p).ToList();

            return new Collection<int>(customerIds);
        }

        public Customer GetCustomer(int customerId)
        {
            string response = _restClient.Get(string.Format("{0}/{1}", _endpoint, customerId));

            var xml = XDocument.Parse(response).Root;

            return new Customer
            {
                CustomerId = (int)xml.Element("ID"),
                FirstName = (string)xml.Element("FIRSTNAME"),
                LastName = (string)xml.Element("LASTNAME"),
                Street = (string)xml.Element("STREET"),
                City = (string)xml.Element("CITY")
            };
        }
    }
}
