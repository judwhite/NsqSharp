using StructureMap.Configuration.DSL;

namespace Customer.Handlers.IoC
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
