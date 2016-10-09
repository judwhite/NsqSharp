using System;
using NsqSharp.Core;

namespace NsqSharp.Tests.TestHelpers
{
    public class TestConsoleLogger : ILogger
    {
        /// <summary>
        /// Writes the output for a logging event.
        /// </summary>
        public void Output(LogLevel logLevel, string message)
        {
            Console.WriteLine("[{0}] {1}", logLevel, message);
        }

        public void Flush()
        {
        }
    }
}
