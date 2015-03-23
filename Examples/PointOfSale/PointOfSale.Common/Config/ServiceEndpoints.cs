using System.Configuration;

namespace PointOfSale.Common.Config
{
    internal class ServiceEndpoints : IServiceEndpoints
    {
        public ServiceEndpoints()
        {
            CustomerEndpoint = ConfigurationManager.AppSettings["CustomerEndpoint"];
            ProductEndpoint = ConfigurationManager.AppSettings["ProductEndpoint"];
            InvoiceEndpoint = ConfigurationManager.AppSettings["InvoiceEndpoint"];
            InvoiceDetailsEndpoint = ConfigurationManager.AppSettings["InvoiceDetailsEndpoint"];
        }

        public string CustomerEndpoint { get; private set; }
        public string ProductEndpoint { get; private set; }
        public string InvoiceEndpoint { get; private set; }
        public string InvoiceDetailsEndpoint { get; private set; }
    }

    public interface IServiceEndpoints
    {
        string CustomerEndpoint { get; }
        string ProductEndpoint { get; }
        string InvoiceEndpoint { get; }
        string InvoiceDetailsEndpoint { get; }
    }
}
