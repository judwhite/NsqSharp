using Customer.Handlers.IoC;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;

namespace Customer.Handlers
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // http://www.thomas-bayer.com/sqlrest/

            var config = new BusConfiguration(
                new StructureMapObjectBuilder(ObjectFactory.Container),
                new NewtonsoftJsonSerializer(typeof(JsonConvert).Assembly),
                defaultThreadsPerHandler: 16,
                defaultNsqlookupdHttpEndpoints: new[] { "127.0.0.1:4161" }
                //onStart: SendMessage
            );

            config.AddMessageHandlers(new[] { typeof(Program).Assembly });

            BusService.Start(config);
        }
    }
}
