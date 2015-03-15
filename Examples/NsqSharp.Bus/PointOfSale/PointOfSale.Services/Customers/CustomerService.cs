using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using PointOfSale.Common.Config;
using PointOfSale.Common.Utils;
using PointOfSale.Services.Customers.Models;

namespace PointOfSale.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly string _endpoint;
        private readonly INemesis _nemesis;

        public CustomerService(IServiceEndpoints serviceEndpoints, INemesis nemesis)
        {
            if (serviceEndpoints == null)
                throw new ArgumentNullException("serviceEndpoints");
            if (nemesis == null)
                throw new ArgumentNullException("nemesis");

            _endpoint = serviceEndpoints.CustomerEndpoint;
            _nemesis = nemesis;
        }

        public Collection<int> GetCustomerIds()
        {
            _nemesis.Invoke();

            var webClient = new WebClient();
            string response = webClient.DownloadString(_endpoint);

            var customerIds = XDocument.Parse(response).Root.Elements("CUSTOMER").Select(p => (int)p).ToList();

            return new Collection<int>(customerIds);
        }

        public Customer GetCustomer(int customerId)
        {
            _nemesis.Invoke();

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
