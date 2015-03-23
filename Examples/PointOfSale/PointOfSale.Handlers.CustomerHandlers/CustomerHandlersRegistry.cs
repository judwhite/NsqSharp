using StructureMap.Configuration.DSL;

namespace PointOfSale.Handlers.CustomerHandlers
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
