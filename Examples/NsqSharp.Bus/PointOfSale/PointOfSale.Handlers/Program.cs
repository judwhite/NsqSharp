using System;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using PointOfSale.Handlers.IoC;
using PointOfSale.Messages;

namespace PointOfSale.Handlers
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // http://www.thomas-bayer.com/sqlrest/

            var config = new BusConfiguration(
                new StructureMapObjectBuilder(ObjectFactory.Container),
                new NewtonsoftJsonSerializer(typeof(JsonConvert).Assembly),
                defaultThreadsPerHandler: 100,
                defaultNsqlookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                onStarted: SendMessage
            );

            config.AddMessageHandlers(new[] { typeof(Program).Assembly });

            BusService.Start(config);
        }

        private static void SendMessage(IBus bus)
        {
            if (bus == null)
                throw new ArgumentNullException("bus");

            bus.Send<GetCustomers>();
        }
    }
}
