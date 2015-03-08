using StructureMap.Configuration.DSL;

namespace PointOfSale.Handlers.CustomerHandlers.IoC
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
        }
    }
}
