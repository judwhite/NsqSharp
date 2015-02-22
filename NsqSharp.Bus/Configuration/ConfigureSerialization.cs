using System;

namespace NsqSharp.Bus.Configuration
{
    internal class ConfigureSerialization : IConfigureSerialization
    {
        public void Json()
        {
            throw new NotImplementedException();
        }

        public void SetDefault(Func<object, string> serializer, Func<string, object> deserializer)
        {
            if (serializer == null)
                throw new ArgumentNullException("serializer");
            if (deserializer == null)
                throw new ArgumentNullException("deserializer");

            throw new NotImplementedException();
        }
    }
}
