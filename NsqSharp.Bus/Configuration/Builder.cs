using System;

namespace NsqSharp.Bus.Configuration
{
    internal class Builder : IBuilder
    {
        public object Build(Type type)
        {
            throw new NotImplementedException();
        }

        public T Build<T>()
        {
            throw new NotImplementedException();
        }
    }
}
