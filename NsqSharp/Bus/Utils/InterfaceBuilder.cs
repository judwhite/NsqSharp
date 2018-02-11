using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NsqSharp.Bus.Utils
{
    /// <summary>
    /// Create a concrete type based on an interface.
    /// </summary>
    public class InterfaceBuilder
    {
        private static readonly Dictionary<Type, Type> _interfaceDynamicTypes = new Dictionary<Type, Type>();
        private static readonly object _interfaceDynamicTypesLocker = new object();
        private static readonly ModuleBuilder _moduleBuilder;

        static InterfaceBuilder()
        {
            var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
#if NETCOREAPP2_0 || NETSTANDARD2_0
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName,AssemblyBuilderAccess.Run);
#else
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
        }

        /// <summary>
        /// Create a concrete object based on an interface.
        /// </summary>
        public static T CreateObject<T>()
        {
            return (T)CreateObject(typeof(T));
        }

        /// <summary>
        /// Create a concrete object based on an interface.
        /// </summary>
        public static object CreateObject(Type interfaceType)
        {
            var type = CreateType(interfaceType);
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Create a concrete type based on an interface.
        /// </summary>
        public static Type CreateType(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException("interfaceType");
            if (!interfaceType.IsInterface)
                throw new ArgumentException(string.Format("Type '{0}' is not an interface.", interfaceType));

            Type type;
            lock (_interfaceDynamicTypesLocker)
            {
                if (!_interfaceDynamicTypes.TryGetValue(interfaceType, out type))
                {
                    type = GetDynamicType(interfaceType);
                    _interfaceDynamicTypes.Add(interfaceType, type);
                }
            }

            return type;
        }

        private static Type GetDynamicType(Type type)
        {
            var typeBuilder = _moduleBuilder.DefineType(type.Name, TypeAttributes.Public);
            typeBuilder.AddInterfaceImplementation(type);

            const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.Virtual;

            foreach (var propertyInfo in GetProperties(type))
            {
                var fieldBuilder = typeBuilder.DefineField(string.Format("__{0}", CamelCase(propertyInfo.Name)),
                    propertyInfo.PropertyType, FieldAttributes.Private);

                var propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, PropertyAttributes.HasDefault,
                    propertyInfo.PropertyType, null);

                var getAccessor = typeBuilder.DefineMethod(string.Format("get_{0}", propertyInfo.Name), getSetAttr,
                    propertyInfo.PropertyType, Type.EmptyTypes);

                var getIl = getAccessor.GetILGenerator();
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, fieldBuilder);
                getIl.Emit(OpCodes.Ret);

                var setAccessor = typeBuilder.DefineMethod(string.Format("set_{0}", propertyInfo.Name), getSetAttr,
                    null, new[] { propertyInfo.PropertyType });

                var setIl = setAccessor.GetILGenerator();
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Stfld, fieldBuilder);
                setIl.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getAccessor);
                propertyBuilder.SetSetMethod(setAccessor);

                typeBuilder.DefineMethodOverride(getAccessor, propertyInfo.GetGetMethod());
                typeBuilder.DefineMethodOverride(setAccessor, propertyInfo.GetSetMethod());
            }

            return typeBuilder.CreateTypeInfo();
        }

        private static string CamelCase(string name)
        {
            if (name.Length == 1)
                return name.ToLower();
            return char.ToLower(name[0]) + name.Substring(1);
        }

        private static IEnumerable<PropertyInfo> GetProperties(Type type)
        {
            var properties = new List<PropertyInfo>();
            properties.AddRange(type.GetProperties());

            foreach (Type baseInterface in type.GetInterfaces())
            {
                properties.AddRange(baseInterface.GetProperties());
            }

            return properties;
        }
    }
}
