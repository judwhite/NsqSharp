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
            var weather = _weatherServiceProxy.GetWeather(message.City);
            _bus.Send(weather);

            throw new System.NotImplementedException();
        }
    }
}
