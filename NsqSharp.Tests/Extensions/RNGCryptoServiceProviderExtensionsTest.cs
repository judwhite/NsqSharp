using System;
using System.Security.Cryptography;
using NsqSharp.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Extensions
{
    [TestFixture]
    public class RNGCryptoServiceProviderExtensionsTest
    {
        private static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

        [Test]
        public void Float64()
        {
            for (int i = 0; i < 10000; i++)
            {
                double value = _rng.Float64();
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, 1);
            }
        }

        [Test]
        public void IntnRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _rng.Intn(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _rng.Intn(-1));
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            _rng.Dispose();
        }
    }
}
