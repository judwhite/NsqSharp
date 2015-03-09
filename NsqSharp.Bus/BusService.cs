using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Utils;
using NsqSharp.Channels;

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

                var exitChan = new Chan<bool>();

                Console.CancelKeyPress += (s, e) => Console_CancelKeyPress(e, exitChan);

                exitChan.Receive();

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
    }
}
