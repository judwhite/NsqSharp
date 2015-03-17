using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using PointOfSale.Common.Config;
using PointOfSale.Common.Utils;
using PointOfSale.Services.Products.Models;

namespace PointOfSale.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly string _endpoint;
        private readonly IRestClient _restClient;

        public ProductService(IServiceEndpoints serviceEndpoints, IRestClient restClient)
        {
            if (serviceEndpoints == null)
                throw new ArgumentNullException("serviceEndpoints");
            if (restClient == null)
                throw new ArgumentNullException("restClient");

            _endpoint = serviceEndpoints.ProductEndpoint;
            _restClient = restClient;
        }

        public Collection<int> GetProductIds()
        {
            string response = _restClient.Get(_endpoint);

            var productIds = XDocument.Parse(response).Root.Elements("PRODUCT").Select(p => (int)p).ToList();

            return new Collection<int>(productIds);
        }

        public Product GetProduct(int productId)
        {
            string response = _restClient.Get(string.Format("{0}/{1}", _endpoint, productId));

            var xml = XDocument.Parse(response).Root;

            return new Product
            {
                ProductId = (int)xml.Element("ID"),
                Name = (string)xml.Element("NAME"),
                Price = (decimal)xml.Element("PRICE")
            };
        }
    }
}
