using System;
using NsqSharp.Attributes;
using NsqSharp.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Extensions
{
    [TestFixture]
    public class OptAttributeExtensionsTest
    {
        [Test]
        public void Coerce()
        {
            var opt = new OptAttribute("testName");
            var coercedVal = opt.Coerce("12ms", typeof(TimeSpan));
            Assert.AreEqual(TimeSpan.FromMilliseconds(12), coercedVal);
        }

        [Test]
        public void InvalidCoerceThrows()
        {
            var opt = new OptAttribute("testName");

            var ex = Assert.Throws<Exception>(() => opt.Coerce("12xs", typeof (TimeSpan)));
            Assert.IsNotNull(ex);
            Assert.IsNotNull(ex.Message);
            Assert.IsTrue(ex.Message.Contains(opt.Name));
        }
    }
}
