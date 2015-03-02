using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Weather.Bootstrap.IoC;

namespace NsqSharp.Bus.Weather.Bootstrap.Bus
{
    public class EndpointConfig : IConfigureThisEndpoint
    {
        public void Init()
        {
            Configure
                .With(new[]
                {
                    typeof(EndpointConfig).Assembly
                })
                .UsingContainer(new StructureMapObjectBuilder(ObjectFactory.Container));

            Configure.Serialization.Json(typeof(JsonConverter).Assembly);
        }
    }
}
