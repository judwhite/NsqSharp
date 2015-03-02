using System;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;
using NUnit.Framework;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;

namespace NsqSharp.Bus.Tests.Configuration
{
    [TestFixture]
    public class StructureMapObjectBuilderTest
    {
        [Test]
        public void RegistrationTest()
        {
            var container = ObjectFactory.Container;

            var weatherServiceProxy = container.GetInstance<IWeatherServiceProxy>();

            Assert.AreEqual("BasicHttpBinding_WeatherService", weatherServiceProxy.Address);
        }

        [Test]
        public void EndpointConfigTest()
        {
            Configure.StartBus<EndpointConfig>();

            var weatherServiceProxy = Configure.Instance.Builder.GetInstance<IWeatherServiceProxy>();

            Assert.AreEqual("BasicHttpBinding_WeatherService", weatherServiceProxy.Address);
        }

        [Test]
        public void ResolveHandleMessageTest()
        {
            Configure.StartBus<EndpointConfig>();

            // TODO
        }
    }

    public class CoreRegistry : Registry
    {
        public CoreRegistry()
        {
            Scan(s =>
            {
                s.AssemblyContainingType<CoreRegistry>();
                s.WithDefaultConventions();
                For<IWeatherServiceProxy>().Add(new WeatherServiceProxy("BasicHttpBinding_WeatherService"));
            });
        }
    }

    public interface IWeatherServiceProxy
    {
        string Address { get; }
    }

    public class WeatherUpdatedMessage
    {
    }

    public class WeatherUpdatedHandler : IHandleMessages<WeatherUpdatedMessage>
    {
        public WeatherUpdatedHandler(IBus bus, IWeatherServiceProxy weatherServiceProxy)
        {
        }

        public void Handle(WeatherUpdatedMessage message)
        {
            throw new NotImplementedException();
        }
    }

    public class WeatherServiceProxy : IWeatherServiceProxy
    {
        private readonly string _address;

        public WeatherServiceProxy(string address)
        {
            _address = address;
        }

        public string Address
        {
            get { return _address; }
        }
    }

    public static class ObjectFactory
    {
        private static readonly Lazy<Container> _containerBuilder =
                    new Lazy<Container>(CreateDefaultContainer, LazyThreadSafetyMode.ExecutionAndPublication);

        public static IContainer Container
        {
            get { return _containerBuilder.Value; }
        }

        private static Container CreateDefaultContainer()
        {
            return new Container(x =>
            {
                x.AddRegistry(new CoreRegistry());
                x.Scan(s =>
                {
                    s.AssembliesFromApplicationBaseDirectory();
                    s.LookForRegistries();
                });
            });
        }
    }

    public class EndpointConfig : IConfigureThisEndpoint
    {
        public void Init()
        {
            Configure
                .With(new[]
                {
                    typeof(EndpointConfig).Assembly
                })
                .UsingContainer(new StructureMapObjectBuilder(ObjectFactory.Container));
            Configure.Serialization.Json(typeof(JsonConvert).Assembly);
        }
    }
}
