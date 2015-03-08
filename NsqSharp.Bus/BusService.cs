using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
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
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        /// <summary>
        /// Starts the bus service.
        /// </summary>
        public static void Start(BusConfiguration busConfiguration)
        {
            if (busConfiguration == null)
                throw new ArgumentNullException("busConfiguration");

            var service = new WindowsService(busConfiguration);

            if (GetConsoleWindow() == IntPtr.Zero)
            {
                ServiceBase.Run(new ServiceBase[] { service });
            }
            else
            {
                service.Start();

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
            if (!string.IsNullOrWhiteSpace(line))
            {
                Console.WriteLine(string.Format("ECHO: {0}", line));
            }
        }
    }
}
