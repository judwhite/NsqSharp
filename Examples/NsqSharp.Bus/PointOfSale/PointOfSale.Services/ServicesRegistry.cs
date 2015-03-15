using StructureMap.Configuration.DSL;

namespace PointOfSale.Services
{
    public class ServicesRegistry : Registry
    {
        public ServicesRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<ServicesRegistry>();
                s.WithDefaultConventions();
            });
        }
    }
}
