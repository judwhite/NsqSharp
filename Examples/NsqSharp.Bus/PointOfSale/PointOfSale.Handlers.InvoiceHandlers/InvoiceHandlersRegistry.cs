using StructureMap.Configuration.DSL;

namespace PointOfSale.Handlers.InvoiceHandlers
{
    public class InvoiceHandlersRegistry : Registry
    {
        public InvoiceHandlersRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<InvoiceHandlersRegistry>();
                s.WithDefaultConventions();
            });
        }
    }
}
