using StructureMap.Configuration.DSL;

namespace PointOfSale.Handlers.Audit
{
    public class CustomerHandlersRegistry : Registry
    {
        public CustomerHandlersRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<CustomerHandlersRegistry>();
                s.WithDefaultConventions();
            });
        }
    }
}
