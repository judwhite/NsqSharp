using PointOfSale.Common.Config;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using StructureMap.Pipeline;

namespace PointOfSale.Common.IoC
{
    public class CommonRegistry : Registry
    {
        public CommonRegistry()
        {
            Scan(s =>
            {
                s.TheCallingAssembly();
                s.WithDefaultConventions();
            });

            For<IAppSettings>(Lifecycles.Singleton).Use(new AppSettings());
            For<IConnectionStrings>(Lifecycles.Singleton).Use(new ConnectionStrings());
            For<IServiceEndpoints>(Lifecycles.Singleton).Use(new ServiceEndpoints());
        }
    }
}
