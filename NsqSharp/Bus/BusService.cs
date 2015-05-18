using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Utils;

namespace NsqSharp.Bus
{
    /// <summary>
    /// Static class to start and stop the bus.
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
        private static HandlerRoutineCallback _onCloseCallback;

        /// <summary>
        /// Starts the bus service.
        /// </summary>
        public static void Start(BusConfiguration busConfiguration)
        {
            if (busConfiguration == null)
                throw new ArgumentNullException("busConfiguration");

            _service = new WindowsService(busConfiguration);

            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly == null)
            {
                // invoked by unit test
                _service.Start();
            }
            else if (GetConsoleWindow() == IntPtr.Zero)
            {
                ServiceBase.Run(new ServiceBase[] { _service });
            }
            else
            {
                _service.Start();

                Trace.WriteLine(string.Format("{0} bus started", Assembly.GetEntryAssembly().GetName().Name));

                _wait = new AutoResetEvent(initialState: false);
                _onCloseCallback = ConsoleCtrlCheck; // prevent callback handler from being GC'd
                SetConsoleCtrlHandler(_onCloseCallback, add: true);
                _wait.WaitOne();
            }
        }

        /// <summary>
        /// Stops the bus. This method should only be invoked in unit tests for teardown.
        /// </summary>
        public static void Stop()
        {
            if (_service != null)
                _service.Stop();
        }

        private static bool _isFirstCancelRequest = true;
        private static bool ConsoleCtrlCheck(CtrlType ctrlType)
        {
            if (ctrlType == CtrlType.CTRL_CLOSE_EVENT)
                return false;

            if (_isFirstCancelRequest)
            {
                Trace.WriteLine("Stopping...");
                _isFirstCancelRequest = false;
                _service.Stop();
                _wait.Set();
                return true;
            }
            else
            {
                Trace.WriteLine("Force Stopping...");
                _wait.Set();
                return true;
            }
        }
    }
}
