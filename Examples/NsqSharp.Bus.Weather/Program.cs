using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Weather.Bootstrap.Bus;
using NsqSharp.Bus.Weather.Messages;

namespace NsqSharp.Bus.Weather
{
    class Program
    {
        static void Main(string[] args)
        {
            //Configure.UseInProcessBus();
            var bus = Configure.StartBus<EndpointConfig>();
            bus.Send(new GetWeather { City = "Austin" });
        }
    }
}
