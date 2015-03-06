namespace Weather.Services
{
    public interface IWeatherServiceProxy
    {
        object GetWeather(string city);
    }
}
