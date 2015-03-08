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
                Console.CancelKeyPress += (s, e) => Console_CancelKeyPress(e, exitChan);

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

                service.Stop();
            }
        }

        private static bool _isFirstCancelRequest = true;
        private static void Console_CancelKeyPress(ConsoleCancelEventArgs e, Chan<bool> stopChan)
        {
            if (_isFirstCancelRequest)
            {
                Console.WriteLine("Stopping... Press ^C again to force quit");
                e.Cancel = true;
                stopChan.Close();
                _isFirstCancelRequest = false;
            }
            else
            {
                Console.WriteLine("^C pressed again, force quitting...");
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
