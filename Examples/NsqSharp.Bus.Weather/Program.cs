using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Weather.Bootstrap.IoC;
using NsqSharp.Bus.Weather.Messages;

namespace NsqSharp.Bus.Weather
{
    class Program
    {
        static void Main()
        {
            var busConfiguration = new BusConfiguration(
                new StructureMapObjectBuilder(ObjectFactory.Container),
                new NewtonsoftJsonSerializer(typeof(JsonConvert).Assembly),
                new[] { "127.0.0.1:4161" }
            );

            busConfiguration.AddMessageHandlers(new[] { typeof(Program).Assembly });

            var bus = busConfiguration.StartBus();

            bus.Send(new GetWeather { City = "Austin" });
        }
    }
}
