using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;

public class Worker1 : BackgroundService
{
    private readonly ILogger<Worker1> _logger;
    //private readonly IBus _bus;
    private readonly IBusConfiguration _busConfig;

    // public Worker1(ILogger<Worker1> logger, IBus bus)
    // {
    //     _logger = logger;
    //     _bus = bus;
    // }
    public Worker1(ILogger<Worker1> logger, IBusConfiguration busConfig) => (_logger, _busConfig) = (logger, busConfig);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
            try {
                // _bus.Start();
                _busConfig.StartBus();
            } catch (Exception ex) {
                _logger.LogError(ex, "Error starting bus");
            }
        }
    }
}
