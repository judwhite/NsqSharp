using PointOfSale.Services.Customers;
using PointOfSale.Services.Invoices;
using PointOfSale.Services.Products;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;

namespace PointOfSale.Services
{
    public class CoreRegistry : Registry
    {
        private const string CustomerEndpoint = "http://www.thomas-bayer.com/sqlrest/CUSTOMER";
        private const string ProductEndpoint = "http://www.thomas-bayer.com/sqlrest/PRODUCT";
        private const string InvoiceEndpoint = "http://www.thomas-bayer.com/sqlrest/INVOICE";
        private const string InvoiceDetailsEndpoint = "http://www.thomas-bayer.com/sqlrest/ITEM";

        public CoreRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<CoreRegistry>();
                s.WithDefaultConventions();
            });

            For<ICustomerService>(Lifecycles.Singleton).Add(() => new CustomerService(CustomerEndpoint));
            For<IInvoiceService>(Lifecycles.Singleton).Add(() => new InvoiceService(InvoiceEndpoint, InvoiceDetailsEndpoint));
            For<IProductService>(Lifecycles.Singleton).Add(() => new ProductService(ProductEndpoint));
        }
    }
}
