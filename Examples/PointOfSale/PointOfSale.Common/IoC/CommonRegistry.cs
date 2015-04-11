using PointOfSale.Common.Config;
using PointOfSale.Common.Utils;
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

            For<IAppSettings>(Lifecycles.Singleton).Use<AppSettings>();
            For<IConnectionStrings>(Lifecycles.Singleton).Use<ConnectionStrings>();
            For<IServiceEndpoints>(Lifecycles.Singleton).Use<ServiceEndpoints>();
            For<IRestClient>(Lifecycles.Singleton).Use<RestClient>();
            For<INemesis>(Lifecycles.Singleton).Use<Nemesis>();
        }
    }
}
