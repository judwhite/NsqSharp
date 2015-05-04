using System;
using NsqSharp.Bus.Utils;
using NsqSharp.Tests.Bus.TestFakes;
using NUnit.Framework;
namespace NsqSharp.Tests.Utils
{
    [TestFixture]
    public class BusConfiguratorTests
    {
        [Test]
        public void TestBuildConfiguratorFailsToStartWithoutRequiredConfiguration()
        {
           //arrange
            var sut = new BusConfigurator();
           //act
            sut.Configure(config => config.UsingMessageAuditor(new MessageAuditorStub()));
           //assert
            Assert.Throws<ArgumentException>(sut.StartBus);
        }


    }
}
