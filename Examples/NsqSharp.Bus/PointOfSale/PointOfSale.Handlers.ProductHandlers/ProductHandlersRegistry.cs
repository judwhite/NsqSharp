using StructureMap.Configuration.DSL;

namespace PointOfSale.Handlers.ProductHandlers
{
    public class ProductHandlersRegistry : Registry
    {
        public ProductHandlersRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<ProductHandlersRegistry>();
                s.WithDefaultConventions();
            });
        }
    }
}
