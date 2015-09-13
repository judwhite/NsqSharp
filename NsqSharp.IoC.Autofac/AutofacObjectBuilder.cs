using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using NsqSharp.Bus.Configuration;

namespace NsqSharp.IoC.Autofac
{
    public class AutofacObjectBuilder : IDependencyResolver
    {
        IContainer _container;

        public AutofacObjectBuilder(IContainer container)
        {
            _container = container;
        }
        public object GetInstance(Type type)
        {
            return _container.Resolve(type);
        }

        public T GetInstance<T>()
        {
            return _container.Resolve<T>();
        }

    }
}
