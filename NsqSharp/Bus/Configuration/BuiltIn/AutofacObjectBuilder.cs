using System;
using System.Linq;
using System.Reflection;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
#if !NETFX_3_5
    /// <summary>
    /// StructureMap object builder. See <see cref="BusConfiguration"/>.
    /// </summary>
    public class AutofacObjectBuilder : IObjectBuilder
    {
        private readonly object _container;
        private readonly object _containerLocker = new object();

        private readonly Type _containerBuilderType;
        private readonly MethodInfo _registerInstanceMethod;
        private readonly MethodInfo _registerTypeMethod;
        private readonly MethodInfo _createRegistrationMethod;

        private readonly MethodInfo _tryResolveMethod;
        private readonly MethodInfo _resolveMethod;

        private readonly object _componentRegistry;
        private readonly MethodInfo _registerMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutofacObjectBuilder"/> class.
        /// See <see cref="BusConfiguration"/>.
        /// </summary>
        /// <param name="container">Autofac IContainer (result of containerBuilder.Build)</param>
        public AutofacObjectBuilder(object container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            _container = container;

            //  Reference: https://groups.google.com/forum/#!topic/autofac/9OptOgmGqrQ

            // Get _container.ComponentRegistry and Register method
            var containerType = _container.GetType();
            var componentRegistryProperty = containerType.GetProperty("ComponentRegistry");
            if (componentRegistryProperty == null)
                throw new Exception("Container.ComponentRegistry property not found");
            var getComponentRegistryProperty = componentRegistryProperty.GetGetMethod();
            if (getComponentRegistryProperty == null)
                throw new Exception("Container.ComponentRegistry property getter not found");
            _componentRegistry = getComponentRegistryProperty.Invoke(_container, null);
            var componentRegistryType = _componentRegistry.GetType();
            _registerMethod = componentRegistryType.GetMethods()
                                                   .SingleOrDefault(p => p.Name == "Register" &&
                                                                         p.GetParameters().Length == 1);
            if (_registerMethod == null)
                throw new Exception("Container.ComponentRegistry.Register property getter not found");

            var autofacAssembly = _container.GetType().Assembly;

            // Get ContainerBuilder type
            _containerBuilderType = autofacAssembly.GetType("Autofac.ContainerBuilder");
            if (_containerBuilderType == null)
                throw new Exception("Autofac.ContainerBuilder type not found");

            // Get ComponentRegistration CreateRegistration method
            var registrationBuilderType = autofacAssembly.GetType("Autofac.Builder.RegistrationBuilder");
            if (registrationBuilderType == null)
                throw new Exception("Autofac.Builder.RegistrationBuilder type not found");

            _createRegistrationMethod =
                registrationBuilderType.GetMethods()
                                       .SingleOrDefault(p => p.Name == "CreateRegistration" &&
                                                             p.GetParameters().Length == 1);
            if (_createRegistrationMethod == null)
                throw new Exception("Autofac.Builder.RegistrationBuilder.CreateRegistration method not found");

            // Get ContainerBuilder RegisterInstance method
            var registrationExtensionsType = autofacAssembly.GetType("Autofac.RegistrationExtensions");
            if (registrationExtensionsType == null)
                throw new Exception("Autofac.RegistrationExtensions type not found");
            _registerInstanceMethod = registrationExtensionsType.GetMethod("RegisterInstance");
            if (_registerInstanceMethod == null)
                throw new Exception("Autofac.RegistrationExtensions.RegisterInstance method not found");

            // Get ContainerBuilder RegisterType method
            _registerTypeMethod =
                registrationExtensionsType.GetMethods()
                                          .SingleOrDefault(p => p.Name == "RegisterType" &&
                                                                p.GetParameters().Length == 2 &&
                                                                p.GetParameters()[1].ParameterType == typeof(Type));
            if (_registerTypeMethod == null)
                throw new Exception("Autofac.RegistrationExtensions.RegisterType method not found");

            // Get _container.TryResolve method
            var resolutionExtensionsType = autofacAssembly.GetType("Autofac.ResolutionExtensions");
            if (resolutionExtensionsType == null)
                throw new Exception("Autofac.ResolutionExtensions type not found");
            _tryResolveMethod =
                resolutionExtensionsType.GetMethods()
                                        .SingleOrDefault(p => p.Name == "TryResolve" &&
                                                              p.GetParameters().Length == 3 &&
                                                              p.GetParameters()[1].ParameterType == typeof(Type));
            if (_tryResolveMethod == null)
                throw new Exception("Autofac.ResolutionExtensions.TryResolve method not found");

            // Get _container.Resolve method
            _resolveMethod =
                resolutionExtensionsType.GetMethods()
                                        .SingleOrDefault(p => p.Name == "Resolve" &&
                                                              p.GetParameters().Length == 2 &&
                                                              p.GetParameters()[1].ParameterType == typeof(Type)
                    );
            if (_resolveMethod == null)
                throw new Exception("Autofac.ResolutionExtensions.Resolve method not found");
        }

        /// <summary>
        /// Creates or finds the registered instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to resolve.</typeparam>
        /// <returns>The registered instance of type <typeparamref name="T"/>.</returns>
        public T GetInstance<T>()
        {
            lock (_containerLocker)
            {
                return (T)GetInstance(typeof(T));
            }
        }

        /// <summary>
        /// Creates or finds the registered instance of the specifid <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns>The registered instance of the specifid <paramref name="type"/>.</returns>
        public object GetInstance(Type type)
        {
            lock (_containerLocker)
            {
                object instance;
                if (TryResolve(type, out instance))
                    return instance;

                RegisterType(type);

                return Resolve(type);
            }
        }

        /// <summary>
        /// Injects an <paramref name="instance"/> of type <typeparamref name="T"/> into the container.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="instance">The instance to inject.</param>
        public void Inject<T>(T instance)
            where T : class
        {
            lock (_containerLocker)
            {
                RegisterInstance(instance);
            }
        }

        private bool TryResolve(Type type, out object instance)
        {
            // object instance;
            // if (_container.TryResolve(type, out instance))
            //     return instance;
            var parameters = new[] { _container, type, null };

            bool success = (bool)_tryResolveMethod.Invoke(null, parameters);
            instance = parameters[2];
            return success;
        }

        private object Resolve(Type type)
        {
            // return _container.Resolve(type);
            var result = _resolveMethod.Invoke(null, new[] { _container, type });
            return result;
        }

        private void RegisterInstance<T>(T instance)
        {
            // var componentRegistration = new ContainerBuilder().RegisterInstance<T>(instance)();
            var containerBuilder = Activator.CreateInstance(_containerBuilderType);
            var registerInstanceMethod = _registerInstanceMethod.MakeGenericMethod(typeof(T));
            var componentRegistration = registerInstanceMethod.Invoke(null, new[] { containerBuilder, instance });

            RegisterComponent(componentRegistration);
        }

        private void RegisterType(Type type)
        {
            // var componentRegistration = new ContainerBuilder().RegisterType(type);
            var containerBuilder = Activator.CreateInstance(_containerBuilderType);
            var componentRegistration = _registerTypeMethod.Invoke(null, new[] { containerBuilder, type });

            RegisterComponent(componentRegistration);
        }

        private void RegisterComponent(object componentRegistration)
        {
            // _container.ComponentRegistry.Register(componentRegistration.CreateRegistration());
            var componentRegistrationType = componentRegistration.GetType();
            var createRegistrationMethod =
                _createRegistrationMethod.MakeGenericMethod(componentRegistrationType.GetGenericArguments());
            var registration = createRegistrationMethod.Invoke(_componentRegistry, new[] { componentRegistration });

            _registerMethod.Invoke(_componentRegistry, new[] { registration });
        }
    }
#endif
}
