using System;
using System.Linq;
using System.Reflection;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
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
        private readonly MethodInfo _getComponentRegistryProperty;
        private readonly MethodInfo _tryResolveMethod;
        private readonly MethodInfo _resolveMethod;

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

            var containerType = _container.GetType();
            var componentRegistryProperty = containerType.GetProperty("ComponentRegistry");
            if (componentRegistryProperty == null)
                throw new Exception("Container.ComponentRegistry property not found");
            _getComponentRegistryProperty = componentRegistryProperty.GetGetMethod();
            if (_getComponentRegistryProperty == null)
                throw new Exception("Container.ComponentRegistry property getter not found");

            var autofacAssembly = _container.GetType().Assembly;

            _containerBuilderType = autofacAssembly.GetType("Autofac.ContainerBuilder");
            if (_containerBuilderType == null)
                throw new Exception("Autofac.ContainerBuilder type not found");

            var registrationBuilderType = autofacAssembly.GetType("Autofac.Builder.RegistrationBuilder");
            if (registrationBuilderType == null)
                throw new Exception("Autofac.Builder.RegistrationBuilder type not found");

            var createRegistrationMethods = registrationBuilderType.GetMethods()
                .Where(p => p.Name == "CreateRegistration")
                .ToList();
            if (createRegistrationMethods.Count == 0)
                throw new Exception("Autofac.Builder.RegistrationBuilder.CreateRegistration method not found");

            _createRegistrationMethod = createRegistrationMethods.SingleOrDefault(p => p.GetParameters().Length == 1);
            if (_createRegistrationMethod == null)
                throw new Exception("Autofac.Builder.RegistrationBuilder.CreateRegistration method not found");

            var registrationExtensionsType = autofacAssembly.GetType("Autofac.RegistrationExtensions");
            if (registrationExtensionsType == null)
                throw new Exception("Autofac.RegistrationExtensions type not found");
            _registerInstanceMethod = registrationExtensionsType.GetMethod("RegisterInstance");
            if (_registerInstanceMethod == null)
                throw new Exception("Autofac.RegistrationExtensions.RegisterInstance method not found");

            _registerTypeMethod =
                registrationExtensionsType.GetMethods()
                    .SingleOrDefault(
                        p =>
                            p.Name == "RegisterType" && p.GetParameters().Length == 2 &&
                            p.GetParameters()[1].ParameterType == typeof(Type)
                    );
            if (_registerTypeMethod == null)
                throw new Exception("Autofac.RegistrationExtensions.RegisterType method not found");

            var resolutionExtensionsType = autofacAssembly.GetType("Autofac.ResolutionExtensions");
            if (resolutionExtensionsType == null)
                throw new Exception("Autofac.ResolutionExtensions type not found");
            _tryResolveMethod =
                resolutionExtensionsType.GetMethods()
                    .SingleOrDefault(
                        p =>
                            p.Name == "TryResolve" && p.GetParameters().Length == 3 &&
                            p.GetParameters()[1].ParameterType == typeof(Type)
                    );
            if (_tryResolveMethod == null)
                throw new Exception("Autofac.ResolutionExtensions.TryResolve method not found");

            _resolveMethod =
                resolutionExtensionsType.GetMethods()
                    .SingleOrDefault(
                        p =>
                            p.Name == "Resolve" && p.GetParameters().Length == 2 &&
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
                // object instance;
                // if (_container.TryResolve(type, out instance))
                //     return instance;
                var parameters = new[] { _container, type, null };

                bool success = (bool)_tryResolveMethod.Invoke(null, parameters);
                if (success)
                    return parameters[2];

                // var componentRegistration = new ContainerBuilder().RegisterType(type);
                var containerBuilder = Activator.CreateInstance(_containerBuilderType);
                object componentRegistration = _registerTypeMethod.Invoke(null, new[] { containerBuilder, type });

                // _container.ComponentRegistry.Register(componentRegistration.CreateRegistration());
                var componentRegistry = _getComponentRegistryProperty.Invoke(_container, null);
                var componentRegistryType = componentRegistry.GetType();
                var registerMethod = componentRegistryType.GetMethods()
                                        .Single(p => p.Name == "Register" && p.GetParameters().Length == 1);

                var componentRegistrationType = componentRegistration.GetType();
                var createRegistrationMethod = _createRegistrationMethod.MakeGenericMethod(
                                                   componentRegistrationType.GenericTypeArguments
                                               );
                var registration = createRegistrationMethod.Invoke(componentRegistry, new[] { componentRegistration });
                registerMethod.Invoke(componentRegistry, new[] { registration });

                // return _container.Resolve(type);
                var result = _resolveMethod.Invoke(null, new[] { _container, type });
                return result;
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
                // Autofac equivalent: 
                //   var cb = new ContainerBuilder().RegisterInstance(instance).As<T>();
                //   _container.ComponentRegistry.Register(cb.CreateRegistration());

                var containerBuilder = Activator.CreateInstance(_containerBuilderType);
                var registerInstanceMethod = _registerInstanceMethod.MakeGenericMethod(typeof(T));
                var componentRegistration = registerInstanceMethod.Invoke(null, new[] { containerBuilder, instance });

                var componentRegistry = _getComponentRegistryProperty.Invoke(_container, null);
                var componentRegistryType = componentRegistry.GetType();
                var registerMethod = componentRegistryType.GetMethods()
                    .Single(p => p.Name == "Register" && p.GetParameters().Length == 1);

                var componentRegistrationType = componentRegistration.GetType();

                var createRegistrationMethod = _createRegistrationMethod.MakeGenericMethod(
                                                   componentRegistrationType.GenericTypeArguments
                                               );
                var registration = createRegistrationMethod.Invoke(componentRegistry, new[] { componentRegistration });
                registerMethod.Invoke(componentRegistry, new[] { registration });
            }
        }
    }
}
