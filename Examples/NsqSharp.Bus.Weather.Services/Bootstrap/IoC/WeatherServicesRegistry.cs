using StructureMap.Configuration.DSL;

namespace NsqSharp.Bus.Weather.Services.Bootstrap.IoC
{
    public class WeatherServicesRegistry : Registry
    {
        public WeatherServicesRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<WeatherServicesRegistry>();
                s.WithDefaultConventions();
                // a4477c4d240353a9b6eec38e4bb00ca2
            });
        }
    }
}
