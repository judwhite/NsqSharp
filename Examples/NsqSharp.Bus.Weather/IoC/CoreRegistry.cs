using StructureMap.Configuration.DSL;

namespace NsqSharp.Bus.Weather.Bootstrap.IoC
{
    public class CoreRegistry : Registry
    {
        public CoreRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<CoreRegistry>();
                s.WithDefaultConventions();
                // a4477c4d240353a9b6eec38e4bb00ca2
            });
        }
    }
}
