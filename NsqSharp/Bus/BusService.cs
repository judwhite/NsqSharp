using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Utils;

namespace NsqSharp.Bus
{
    /// <summary>
    /// Start the bus service.
    /// </summary>
    public static class BusService
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(HandlerRoutineCallback handler, bool add);

        internal delegate bool HandlerRoutineCallback(CtrlType dwCtrlType);

        internal enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static WindowsService _service;
        private static AutoResetEvent _wait;

        /// <summary>
        /// Starts the bus service.
        /// </summary>
        public static void Start(BusConfiguration busConfiguration)
        {
            if (busConfiguration == null)
                throw new ArgumentNullException("busConfiguration");

            _service = new WindowsService(busConfiguration);

            if (GetConsoleWindow() == IntPtr.Zero)
            {
                ServiceBase.Run(new ServiceBase[] { _service });
            }
            else
            {
                _service.Start();

                Console.WriteLine("{0} bus started", Assembly.GetEntryAssembly().GetName().Name);

                _wait = new AutoResetEvent(initialState: false);
                SetConsoleCtrlHandler(ConsoleCtrlCheck, add: true);
                _wait.WaitOne();
            }
        }

        private static bool _isFirstCancelRequest = true;
        private static bool ConsoleCtrlCheck(CtrlType ctrlType)
        {
            if (ctrlType == CtrlType.CTRL_CLOSE_EVENT)
                return false;

            if (_isFirstCancelRequest)
            {
                Console.WriteLine("Stopping...");
                _service.Stop();
                _isFirstCancelRequest = false;
                _wait.Set();
                return true;
            }
            else
            {
                Console.WriteLine("Force Stopping...");
                _wait.Set();
                return true;
            }
        }
    }
}
