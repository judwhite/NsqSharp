using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using PointOfSale.Services.Products.Models;

namespace PointOfSale.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly string _endpoint;

        public ProductService(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException("endpoint");

            _endpoint = endpoint;
        }

        public Collection<int> GetProductIds()
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(_endpoint);

            var productIds = XDocument.Parse(response).Root.Elements("PRODUCT").Select(p => (int)p).ToList();

            return new Collection<int>(productIds);
        }

        public Product GetProduct(int productId)
        {
            var webClient = new WebClient();
            string response = webClient.DownloadString(string.Format("{0}/{1}", _endpoint, productId));

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
