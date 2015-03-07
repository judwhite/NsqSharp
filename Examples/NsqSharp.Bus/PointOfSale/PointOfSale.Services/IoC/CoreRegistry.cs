using StructureMap.Configuration.DSL;

namespace PointOfSale.Services.IoC
{
    public class CoreRegistry : Registry
    {
        public CoreRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<CoreRegistry>();
                s.WithDefaultConventions();
            });

            For<ICustomerService>().Add(() => new CustomerService("http://www.thomas-bayer.com/sqlrest/CUSTOMER"));
        }
    }
}
