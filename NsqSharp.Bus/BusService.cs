using System;
using System.ServiceProcess;
using System.Threading;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Utils;
using NsqSharp.Channels;
using NsqSharp.Go;

namespace NsqSharp.Bus
{
    /// <summary>
    /// Start the bus service.
    /// </summary>
    public static class BusService
    {
        /// <summary>
        /// Starts the bus service.
        /// </summary>
        public static void Start(BusConfiguration busConfiguration)
        {
            if (busConfiguration == null)
                throw new ArgumentNullException("busConfiguration");

            var service = new WindowsService(busConfiguration);

            if (!Environment.UserInteractive)
            {
                ServiceBase.Run(new ServiceBase[] { service });
            }
            else
            {
                service.Start();

                var wait = new AutoResetEvent(initialState: false);

                var inputChan = new Chan<string>();
                var exitChan = new Chan<bool>();

                GoFunc.Run(() => ReadStdin(inputChan));
                Console.CancelKeyPress += delegate { exitChan.Close(); };

                bool doLoop = true;

                using (var select = Select
                                    .CaseReceive(inputChan, ProcessInput)
                                    .CaseReceive(exitChan, o => doLoop = false)
                                    .NoDefault(defer: true))
                {
                    while (doLoop)
                    {
                        select.Execute();
                    }
                }
                wait.WaitOne();
            }
        }

        private static void ReadStdin(ISendOnlyChan<string> inputChan)
        {
            while (true)
            {
                string line = Console.ReadLine();
                inputChan.Send(line);
            }
        }

        private static void ProcessInput(string line)
        {
            // TODO
            Console.WriteLine(string.Format("ECHO: {0}", line));
        }
    }
}
