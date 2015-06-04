using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Web.Hosting;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus.Utils
{
    internal class WindowsService : ServiceBase, IRegisteredObject
    {
        private readonly BusConfiguration _busConfiguration;
        private int _stop;

        public WindowsService(BusConfiguration busConfiguration)
        {
            if (busConfiguration == null)
                throw new ArgumentNullException("busConfiguration");

            CanStop = true;
            CanShutdown = true;

            _busConfiguration = busConfiguration;
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            if (Interlocked.CompareExchange(ref _stop, value: 1, comparand: 0) == 1)
            {
                return;
            }

            StopBus();
        }

        protected override void OnShutdown()
        {
            OnStop();
        }

        public void Start()
        {
            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly != null)
            {
                EventLog.Log = "Application";
                EventLog.Source = entryAssembly.GetName().Name;
            }
            else
            {
                EventLog.Log = "Application";
                EventLog.Source = "NsqSharp.Bus Unit Tests";
            }

            _busConfiguration.StartBus();

            Trace.WriteLine(string.Format("{0} bus started", Assembly.GetEntryAssembly().GetName().Name));
        }

        private void StopBus()
        {
            _busConfiguration.StopBus();
        }

        public void Stop(bool immediate)
        {
            OnStop();

            HostingEnvironment.UnregisterObject(this);
        }
    }
}
