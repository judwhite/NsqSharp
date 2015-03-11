using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.Bus.Utils
{
    internal class WindowsService : ServiceBase
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
            if (Interlocked.CompareExchange(ref _stop, value: 1, comparand: 0) == 1)
            {
                return;
            }

            StopBus();
        }

        public void Start()
        {
            EventLog.Log = "Application";
            EventLog.Source = Assembly.GetEntryAssembly().GetName().Name;

            _busConfiguration.StartBus();
        }

        private void StopBus()
        {
            _busConfiguration.StopBus();
        }
    }
}
