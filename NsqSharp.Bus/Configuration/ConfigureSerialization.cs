using System;
using System.IO;
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

    internal class ConfigureSerialization : IConfigureSerialization
    {
        internal Func<object, string> _defaultSerializer;
        internal Func<string, object> _defaultDeserializer;

        public void Json()
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(dir, "Newtonsoft.Json.dll");
            if (files.Length == 1)
            {
                var jsonAssembly = Assembly.LoadFrom(files[0]);
                Json(jsonAssembly);
            }
            else
            {
                throw new Exception(string.Format("Newtonsoft.Json.dll not found in directory {0}", dir));
            }
        }

        public void Json(Assembly jsonAssembly)
        {
            if (jsonAssembly == null)
                throw new ArgumentNullException("jsonAssembly");

            var jsonConvertType = jsonAssembly.GetType("Newtonsoft.Json.JsonConvert", throwOnError: true);
            var jsonConvertMethods = jsonConvertType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            Func<object, string> serializer = null;
            Func<string, object> deserializer = null;

            foreach (var method in jsonConvertMethods)
            {
                bool isSerializeObject = (method.Name == "SerializeObject");
                bool isDeserializeObject = (method.Name == "DeserializeObject");

                if (isSerializeObject || isDeserializeObject)
                {
                    var genericArgs = method.GetGenericArguments();
                    var parameters = method.GetParameters();

                    if (genericArgs.Length == 0 && parameters.Length == 1)
                    {
                        if (isSerializeObject)
                        {
                            var serializeMethod = method;
                            serializer = o => (string)serializeMethod.Invoke(null, new[] { o });
                        }
                        else
                        {
                            var deserializeMethod = method;
                            deserializer = s => deserializeMethod.Invoke(null, new object[] { s });
                        }
                    }
                }
            }

            if (serializer == null)
                throw new Exception("Cannot find Newtonsoft.Json.JsonConvert.SerializeObject static method");
            if (deserializer == null)
                throw new Exception("Cannot find Newtonsoft.Json.JsonConvert.DeserializeObject static method");

            SetDefault(serializer, deserializer);
        }

        public void SetDefault(Func<object, string> serializer, Func<string, object> deserializer)
        {
            if (serializer == null)
                throw new ArgumentNullException("serializer");
            if (deserializer == null)
                throw new ArgumentNullException("deserializer");

            if (_defaultSerializer != null)
                throw new Exception("default serializer already set");
            if (_defaultDeserializer != null)
                throw new Exception("default deserializer already set");

            _defaultSerializer = serializer;
            _defaultDeserializer = deserializer;
        }
    }
}
