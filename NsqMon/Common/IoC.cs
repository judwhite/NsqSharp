using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NsqMon.Common.ApplicationServices;

namespace NsqMon.Common
{
    /// <summary>
    /// Inversion of Control
    /// </summary>
    public static class IoC
    {
        private static readonly Dictionary<Type, ConstructorInfo> _types = new Dictionary<Type, ConstructorInfo>();
        private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private static readonly object _locker = new object();

        /// <summary>
        /// Initializes the <see cref="IoC"/> class.
        /// </summary>
        static IoC()
        {
            RegisterInstance<IEventAggregator>(new EventAggregator());
        }

        /// <summary>
        /// Registers an instance of a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="instance">The instance.</param>
        public static void RegisterInstance<T>(object instance)
        {
            lock (_locker)
            {
                _instances.Add(typeof(T), (T)instance);
            }
        }

        /// <summary>
        /// Registers an instance of a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="instance">The instance.</param>
        public static void RegisterInstance(Type type, object instance)
        {
            lock (_locker)
            {
                _instances.Add(type, instance);
            }
        }

        /// <summary>
        /// Registers an interface and implementation type.
        /// </summary>
        /// <typeparam name="TInterface">The type of the interface.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        public static void RegisterType<TInterface, TImplementation>()
            where TImplementation : TInterface
        {
            RegisterType(typeof(TInterface), typeof(TImplementation));
        }

        /// <summary>
        /// Registers an interface and implementation type.
        /// </summary>
        /// <param name="interfaceType">Type of the interface.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        public static void RegisterType(Type interfaceType, Type implementationType)
        {
            ConstructorInfo[] ctors = implementationType.GetConstructors();
            if (ctors.Length != 1)
            {
                ctors = ctors.Where(p => p.GetParameters().Length != 0).ToArray();
                if (ctors.Length != 1)
                    throw new Exception(string.Format("Type '{0}' must contain only one public constructor.", implementationType));
            }

            lock (_locker)
            {
                _types.Add(interfaceType, ctors[0]);
            }
        }

        /// <summary>
        /// Resolves an instance of a type.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>An instance of the type.</returns>
        public static T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        private static object Resolve(Type interfaceType)
        {
            lock (_locker)
            {
                if (_instances.ContainsKey(interfaceType))
                    return _instances[interfaceType];

                if (_types.ContainsKey(interfaceType))
                {
                    ConstructorInfo ctor = _types[interfaceType];
                    ParameterInfo[] parameters = ctor.GetParameters();
                    if (parameters == null || parameters.Length == 0)
                        return ctor.Invoke(null);

                    object[] paramValues = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo parameter = parameters[i];
                        paramValues[i] = Resolve(parameter.ParameterType);
                    }

                    return ctor.Invoke(paramValues);
                }
            }

            if (interfaceType.IsClass)
            {
                RegisterType(interfaceType, interfaceType);
                return Resolve(interfaceType);
            }

            throw new Exception(string.Format("Type '{0}' not registered.", interfaceType));
        }

        /// <summary>Clears all IoC registrations.</summary>
        public static void ClearAllRegistrations()
        {
            lock (_locker)
            {
                _types.Clear();

                foreach (var item in new Dictionary<Type, object>(_instances))
                {
                    if (item.Key != typeof(IEventAggregator))
                    {
                        _instances.Remove(item.Key);
                    }
                }
            }
        }
    }
}
