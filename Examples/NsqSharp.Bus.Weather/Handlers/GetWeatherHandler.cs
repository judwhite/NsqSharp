using NsqSharp.Bus.Weather.Messages;
using NsqSharp.Bus.Weather.Services;

namespace NsqSharp.Bus.Weather.Handlers
{
    public class GetWeatherHandler : IHandleMessages<GetWeather>
    {
        private readonly IBus _bus;
        private readonly IWeatherServiceProxy _weatherServiceProxy;

        public GetWeatherHandler(IBus bus, IWeatherServiceProxy weatherServiceProxy)
        {
            _bus = bus;
            _weatherServiceProxy = weatherServiceProxy;
        }

        public void Handle(GetWeather message)
        {
            throw new System.NotImplementedException();
        }
    }
}
