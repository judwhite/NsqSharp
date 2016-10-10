using System;
using System.Threading;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Loggers;
using NUnit.Framework;

namespace NsqSharp.Tests.Utils.Loggers
{
    public class ConsoleLoggerTest
    {
        [Test]
        public void TestConsoleLoggerThreadSafety()
        {
            var consoleLogger = new ConsoleLogger(LogLevel.Debug);
            var wg = new WaitGroup();
            wg.Add(100);
            var rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                int n = rnd.Next(100, 65536);
                var msg = new string('.', n);
                var t = new Thread(() =>
                {
                    consoleLogger.Output(LogLevel.Warning, msg);
                    wg.Done();
                });
                t.IsBackground = true;
                t.Start();
            }

            wg.Wait();
        }
    }
}
