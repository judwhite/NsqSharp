using System;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Generic object builder using assigned container.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// Builds an object.
        /// </summary>
        /// <param name="type">The type of the object.</param>
        /// <returns>The constructred object with dependencies resolved.</returns>
        object Build(Type type);

        /// <summary>
        /// Builds an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The constructred object with dependencies resolved.</returns>
        T Build<T>();
    }

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
