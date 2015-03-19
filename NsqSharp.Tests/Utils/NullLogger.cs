using NsqSharp.Core;

namespace NsqSharp.Tests.Utils
{
    public class NullLogger : ILogger
    {
        public void Output(LogLevel logLevel, string message)
        {
        }

        public void Flush()
        {
        }
    }
}
