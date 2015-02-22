using System;

namespace NsqSharp.Bus.Configuration
{
    internal class ConfigureComponents : IConfigureComponents
    {
        public void ConfigureComponent<T>(Func<T> componentBuilder, DependencyLifecycle dependencyLifecycle)
        {
            throw new NotImplementedException();
        }

        public void ConfigureComponent<T>(DependencyLifecycle dependencyLifecycle)
        {
            throw new NotImplementedException();
        }
    }
}
