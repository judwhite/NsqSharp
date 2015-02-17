using System;

namespace NsqSharp.Bus
{
    internal class Configuration : IConfiguration
    {
        public Configuration(BusType busType)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<THandler, TMessage>(string topic, string channel, params string[] lookupHttpAddresses)
            where THandler : IHandleMessages<TMessage>
        {
            throw new System.NotImplementedException();
        }

        public void RegisterDestination<TMessage>(string topic, params string[] nsqdTcpAddresses)
        {
            throw new System.NotImplementedException();
        }
    }
}
