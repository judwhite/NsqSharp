using Microsoft.Extensions.Logging;

public class RyansLogger : NsqSharp.Core.ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }

    public void Flush()
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        throw new NotImplementedException();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine("hi RYAN");
    }

    public void Output(NsqSharp.Core.LogLevel loglevel, string message)
    {
        throw new NotImplementedException();
    }
}