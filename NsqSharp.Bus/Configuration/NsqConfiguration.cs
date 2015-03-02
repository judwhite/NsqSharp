using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Configuration settings.
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Register an IoC container with the bus.
        /// </summary>
        /// <param name="objectBuilder">The <see cref="IObjectBuilder"/>. See <see cref="StructureMapObjectBuilder"/>
        /// for a built in implementation.</param>
        IConfiguration UsingContainer(IObjectBuilder objectBuilder);

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="channel"></param>
        /// <param name="lookupHttpAddresses"></param>
        IConfiguration Subscribe<THandler, TMessage>(string topic, string channel, params string[] lookupHttpAddresses)
            where THandler : IHandleMessages<TMessage>;

        /// <summary>
        /// TODO
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="topic"></param>
        /// <param name="nsqdTcpAddresses"></param>
        IConfiguration RegisterDestination<TMessage>(string topic, params string[] nsqdTcpAddresses);
    }

    internal class NsqConfiguration : IConfiguration
    {
        private readonly NsqBus _bus;
        private readonly Dictionary<Type, Type> _handlers; // handlerType, messageType

        public NsqConfiguration(IEnumerable<Assembly> assemblies)
        {
            _bus = new NsqBus();
            _handlers = new Dictionary<Type, Type>();

            ScanAssemblies(assemblies);
        }

        public void ScanAssemblies(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(p => p.GetTypes());

            foreach (var type in types)
            {
                Type messageType;
                if (IsMessageHandler(type, out messageType))
                {
                    if (!_handlers.ContainsKey(type))
                    {
                        _handlers.Add(type, messageType);
                    }
                }
            }
        }

        private static bool IsMessageHandler(Type type, out Type messageType)
        {
            messageType = null;

            if (!type.IsClass)
                return false;

            foreach (var interf in type.GetInterfaces())
            {
                if (IsMessageHandlerInterface(interf, out messageType))
                    return true;

                foreach (var subInterf in interf.GetInterfaces())
                {
                    if (IsMessageHandlerInterface(subInterf, out messageType))
                        return true;
                }
            }

            return false;
        }

        private static bool IsMessageHandlerInterface(Type interf, out Type messageType)
        {
            if (interf.IsGenericType && interf.GetGenericTypeDefinition() == typeof(IHandleMessages<>))
            {
                messageType = interf.GetGenericArguments()[0];
                return true;
            }
            messageType = null;
            return false;
        }

        public IConfiguration Subscribe<THandler, TMessage>(string topic, string channel, params string[] lookupHttpAddresses)
            where THandler : IHandleMessages<TMessage>
        {
            throw new System.NotImplementedException();
            //return this;
        }

        public IConfiguration RegisterDestination<TMessage>(string topic, params string[] nsqdTcpAddresses)
        {
            throw new System.NotImplementedException();
            //return this;
        }

        public IConfiguration UsingContainer(IObjectBuilder objectBuilder)
        {
            objectBuilder.Inject<IBus>(_bus);
            Configure.Instance.Builder = objectBuilder;
            return this;
        }

        public IBus GetBus()
        {
            return _bus;
        }

        internal IBus StartBus()
        {
            return _bus.Start(_handlers);
        }
    }
}
