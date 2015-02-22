using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;
using NUnit.Framework;

namespace NsqSharp.Bus.Tests.Configuration
{
    [TestFixture]
    public class ConfigureSerializationTest
    {
        [Test]
        public void Json()
        {
            Configure.Serialization.Json(typeof(JsonConvert).Assembly);
        }
    }
}
