using System;
using System.Reflection;
using System.Text;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>
    /// Convenience class for creating a Newtonsoft.Json message serializer. See <see cref="BusConfiguration"/>.
    /// </summary>
    public class NewtonsoftJsonSerializer : IMessageSerializer
    {
        private readonly Func<object, byte[]> _serializer;
        private readonly Func<Type, byte[], object> _deserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewtonsoftJsonSerializer"/> class. See <see cref="BusConfiguration"/>.
        /// </summary>
        /// <param name="newtonsoftJsonAssembly">The Newtonsoft.Json assembly; typically typeof(JsonConvert).Assembly.</param>
        public NewtonsoftJsonSerializer(Assembly newtonsoftJsonAssembly)
        {
            if (newtonsoftJsonAssembly == null)
                throw new ArgumentNullException("newtonsoftJsonAssembly");

            var jsonConvertType = newtonsoftJsonAssembly.GetType("Newtonsoft.Json.JsonConvert", throwOnError: true);
            var jsonConvertMethods = jsonConvertType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            Func<object, byte[]> serializer = null;
            Func<Type, byte[], object> deserializer = null;

            foreach (var method in jsonConvertMethods)
            {
                bool isSerializeObject = (method.Name == "SerializeObject");
                bool isDeserializeObject = (method.Name == "DeserializeObject");

                if (isSerializeObject || isDeserializeObject)
                {
                    var genericArgs = method.GetGenericArguments();
                    var parameters = method.GetParameters();

                    if (genericArgs.Length == 0)
                    {
                        if (isSerializeObject && parameters.Length == 1)
                        {
                            if (parameters[0].ParameterType == typeof(object))
                            {
                                var serializeMethod = method;
                                serializer = obj =>
                                    Encoding.UTF8.GetBytes((string)serializeMethod.Invoke(null, new[] { obj }));
                            }
                        }
                        else if (isDeserializeObject && parameters.Length == 2)
                        {
                            if (parameters[0].ParameterType == typeof(string) && parameters[1].ParameterType == typeof(Type))
                            {
                                var deserializeMethod = method;
                                deserializer = (type, byteArray) =>
                                    deserializeMethod.Invoke(null, new object[] { type, Encoding.UTF8.GetString(byteArray) });
                            }
                        }
                    }
                }
            }

            if (serializer == null)
                throw new Exception("Cannot find Newtonsoft.Json.JsonConvert.SerializeObject static method");
            if (deserializer == null)
                throw new Exception("Cannot find Newtonsoft.Json.JsonConvert.DeserializeObject static method");

            _serializer = serializer;
            _deserializer = deserializer;
        }

        /// <summary>
        /// Serializes the specified <paramref name="value"/> to a byte array.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized value.</returns>
        public byte[] Serialize(object value)
        {
            return _serializer(value);
        }

        /// <summary>
        /// Deserializes the specified <paramref name="value"/> to an object of type <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type of the deserialized object.</param>
        /// <param name="value">The value to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        public object Deserialize(Type type, byte[] value)
        {
            return _deserializer(type, value);
        }
    }
}
