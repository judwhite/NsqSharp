using System;
using System.Reflection;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Serialization configuration
    /// </summary>
    public interface IConfigureSerialization
    {
        /// <summary>
        /// Attempts to find a compatible version of Newtonsoft.Json.
        /// </summary>
        void Json();

        /// <summary>
        /// Attempts to find a compatible version of Newtonsoft.Json.
        /// </summary>
        void Json(Assembly jsonAssembly);

        /// <summary>
        /// Sets the default serialization/deserialization methods.
        /// </summary>
        /// <param name="serializer">The default serialization method.</param>
        /// <param name="deserializer">The default deserialization method.</param>
        void SetDefault(Func<object, string> serializer, Func<string, object> deserializer);
    }
}
