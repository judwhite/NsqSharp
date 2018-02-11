using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Web.Hosting;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Utils;

namespace NsqSharp.Bus
{
    /// <summary>
    /// Static class to start and stop the bus.
    /// </summary>
    public static class BusService
    {
        private static WindowsService _service;
        private static AutoResetEvent _wait;
        private static HandlerRoutineCallback _onCloseCallback;

        /// <summary>Starts the bus service.</summary>
        /// <remarks>
        ///     Note: This is a blocking call for Console Applications running in interactive mode. This method will block
        ///     until Ctrl+C is pressed and then initiate a clean shutdown.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="busConfiguration"/> is <c>null</c>.
        /// </exception>
        /// <param name="busConfiguration">The bus configuration.</param>
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
            else if (HostingEnvironment.IsHosted)
            {
                // Hosted in IIS
                _service.Start();
                HostingEnvironment.RegisterObject(_service);
            }
            else if (NativeMethods.GetConsoleWindow() != IntPtr.Zero)
            {
                // Console application
                _service.Start();

                _wait = new AutoResetEvent(initialState: false);
                _onCloseCallback = ConsoleCtrlCheck; // prevent callback handler from being GC'd
                NativeMethods.SetConsoleCtrlHandler(_onCloseCallback, add: true);
                _wait.WaitOne();
            }
            else if (!Environment.UserInteractive)
            {
                // Windows service
                ServiceBase.Run(new ServiceBase[] { _service });
            }
            else
            {
                // Windows application
                _service.Start();

                AppDomain.CurrentDomain.ProcessExit += (s, e) => Stop();
            }
        }

        /// <summary>
        ///     <para>Stops the bus permanently.</para>
        ///     <para>Windows Services - This method is called when the service is stopped or restarted. You do not need to
        ///     call it directly.</para>
        ///     <para>ASP.NET Applications (including Web API and WCF) - May call this method directly if clean exit is a high
        ///     priority. If not called directly it will be called automatically when the hosting enviroment shuts down the
        ///     application (Application Pool stop/restart, Site stop/restart, etc), although the timeout period before a
        ///     forced shutdown is not guaranteed.</para>
        ///     <para>Console Applications - May call this method directly, otherwise it will be called automatically when the
        ///     user presses Ctrl+C.</para>
        ///     <para>Windows Forms/WPF Applications - May call this method directly if clean exit is necessary, otherwise it
        ///     will be called with a short timeout (~3s) when the process exits normally (not force killed).</para>
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
