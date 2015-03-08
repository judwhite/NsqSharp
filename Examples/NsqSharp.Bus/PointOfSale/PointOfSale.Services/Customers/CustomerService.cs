using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using PointOfSale.Services.Customers.Models;

namespace PointOfSale.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly string _endpoint;

        public CustomerService(string endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");

            _endpoint = endpoint;
        }

        public Collection<int> GetCustomerIds()
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(_endpoint);

            var customerIds = XDocument.Parse(response).Root.Elements("CUSTOMER").Select(p => (int)p).ToList();

            return new Collection<int>(customerIds);
        }

        public Customer GetCustomer(int customerId)
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(string.Format("{0}/{1}", _endpoint, customerId));

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
