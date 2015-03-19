using System;
using System.Linq;
using System.Reflection;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>
    /// StructureMap object builder. See <see cref="BusConfiguration"/>.
    /// </summary>
    public class StructureMapObjectBuilder : IObjectBuilder
    {
        private readonly object _container;
        private readonly MethodInfo _getInstanceType;
        private readonly MethodInfo _injectType;

        /// <summary>
        /// Initializes a new instance of the <see cref="StructureMapObjectBuilder"/> class.
        /// See <see cref="BusConfiguration"/>.
        /// </summary>
        /// <param name="objectFactoryContainer">StructureMap ObjectFactory.Container</param>
        public StructureMapObjectBuilder(object objectFactoryContainer)
        {
            if (objectFactoryContainer == null)
                throw new ArgumentNullException("objectFactoryContainer");

            _container = objectFactoryContainer;

            var methods = _container.GetType().GetMethods();

            // GetInstance

            var getInstanceMethods = methods.Where(p => p.Name == "GetInstance").ToList();

            foreach (var method in getInstanceMethods)
            {
                var methodParams = method.GetParameters();
                if (methodParams.Length == 1 && methodParams[0].ParameterType == typeof(Type))
                {
                    _getInstanceType = method;
                    break;
                }
            }

            if (_getInstanceType == null)
            {
                throw new ArgumentException(
                    "Could not find method with signature 'object GetInstance(Type type)'", "objectFactoryContainer");
            }

            // Inject

            _injectType = methods.Single(p => p.Name == "Inject" && p.GetParameters().Length == 2
                && p.GetParameters()[1].ParameterType == typeof(object));
        }

        /// <summary>
        /// Creates or finds the registered instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The registered instance of type <typeparamref name="T"/>.</returns>
        public T GetInstance<T>()
        {
            return (T)_getInstanceType.Invoke(_container, new object[] { typeof(T) });
        }

        /// <summary>
        /// Creates or finds the registered instance of the specifid <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns>The registered instance of the specifid <paramref name="type"/>.</returns>
        public object GetInstance(Type type)
        {
            return _getInstanceType.Invoke(_container, new object[] { type });
        }

        /// <summary>
        /// Injects an <paramref name="instance"/> of type <typeparamref name="T"/> into the container.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="instance">The instance to inject.</param>
        public void Inject<T>(T instance)
            where T : class
        {
            _injectType.Invoke(_container, new object[] { typeof(T), instance });
        }
    }
}
