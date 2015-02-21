using System;
using System.Collections.Generic;
using System.Reflection;

namespace NsqSharp.Bus.Configuration
{
    internal class NsqConfiguration : IConfiguration
    {
        public NsqConfiguration(IEnumerable<Assembly> assembliesToScan)
        {
            throw new NotImplementedException();
        }

        public IConfiguration Subscribe<THandler, TMessage>(string topic, string channel, params string[] lookupHttpAddresses)
            where THandler : IHandleMessages<TMessage>
        {
            throw new System.NotImplementedException();
        }

        public IConfiguration RegisterDestination<TMessage>(string topic, params string[] nsqdTcpAddresses)
        {
            throw new System.NotImplementedException();
        }

        public IConfiguration UsingContainer(IObjectBuilder objectBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
