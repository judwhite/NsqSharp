using System;
using NsqSharp.Core;

namespace NsqSharp.Tests.TestHelpers
{
    public class TestConsoleLogger : Core.ILogger
    {
        /// <summary>
        /// Writes the output for a logging event.
        /// </summary>
        public void Output(Core.LogLevel Core.LogLevel, string message)
        {
            Console.WriteLine("[{0}] {1}", Core.LogLevel, message);
        }

        public void Flush()
        {
        }
    }
}
