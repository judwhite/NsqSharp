using System;
using System.Text;
using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;

namespace PingPong.Configuration
{
    public class MessageSerializer : IMessageSerializer
    {
        // Can also use NsqSharp.Bus.Configuration.BuiltIn.NewtonsoftJsonSerializer instead of writing your own
        // if you use Newtonsoft.Json. IMessageSerializer implementation shown to demonstrate how any serializer
        // could be used.

        public object Deserialize(Type type, byte[] value)
        {
            string json = Encoding.UTF8.GetString(value);
            return JsonConvert.DeserializeObject(json, type);
        }

        public byte[] Serialize(object value)
        {
            string json = JsonConvert.SerializeObject(value);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
