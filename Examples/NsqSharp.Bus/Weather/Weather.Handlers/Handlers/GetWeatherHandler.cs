using NsqSharp.Bus;
using Weather.Handlers.Messages;
using Weather.Services;

namespace Weather.Handlers.Handlers
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
