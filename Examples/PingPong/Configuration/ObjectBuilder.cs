using System;
using NsqSharp.Bus.Configuration;
using StructureMap;

namespace PingPong.Configuration
{
    public class ObjectBuilder : IObjectBuilder
    {
        // Can also use NsqSharp.Bus.Configuration.BuiltIn.StructureMapObjectBuilder instead of writing your own
        // if you use StructureMap. IObjectBuilder implementation shown to demonstrate how any dependency injection
        // container could be used.

        private readonly Container _container;

        public ObjectBuilder(Container container)
        {
            _container = container;
        }

        public object GetInstance(Type type)
        {
            return _container.GetInstance(type);
        }

        public T GetInstance<T>()
        {
            return _container.GetInstance<T>();
        }

        public void Inject<T>(T instance)
            where T : class
        {
            _container.Inject(typeof(T), instance);
        }
    }
}
