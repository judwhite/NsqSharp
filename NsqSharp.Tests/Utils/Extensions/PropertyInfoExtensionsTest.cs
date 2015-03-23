using System;
using NsqSharp.Utils.Attributes;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Utils.Extensions
{
    [TestFixture]
    public class PropertyInfoExtensionsTest
    {
        public class TestPropertyClass
        {
            [UnitTest("test1")]
            public string OneAttributeProperty { get; set; }

            public string NoAttributesProperty { get; set; }

            [UnitTest("test2")]
            [Max("test3")]
            public string DifferentAttributesProperty { get; set; }

            [UnitTest("test4")]
            [UnitTest("test5")]
            public string DuplicateAttributesProperty { get; set; }
        }

        public class Subclass : TestPropertyClass
        {
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
        public class UnitTestAttribute : Attribute
        {
            public string Value { get; private set; }

            public UnitTestAttribute(string value)
            {
                Value = value;
            }
        }

        [Test]
        public void TestGetReturnsAttribute()
        {
            var propertyInfo = typeof(TestPropertyClass).GetProperty("OneAttributeProperty");

            var attr = propertyInfo.Get<UnitTestAttribute>();

            Assert.IsNotNull(attr);
            Assert.AreEqual("test1", attr.Value);
        }

        [Test]
        public void TestGetReturnsNullIfNoMatch()
        {
            var propertyInfo = typeof(TestPropertyClass).GetProperty("OneAttributeProperty");

            var attr = propertyInfo.Get<MaxAttribute>();

            Assert.IsNull(attr);
        }

        [Test]
        public void TestGetReturnsNullIfNoAttributes()
        {
            var propertyInfo = typeof(TestPropertyClass).GetProperty("NoAttributesProperty");

            var attr = propertyInfo.Get<UnitTestAttribute>();

            Assert.IsNull(attr);
        }

        [Test]
        public void TestGetThrowsIfMultipleAttributes()
        {
            var propertyInfo = typeof(TestPropertyClass).GetProperty("DuplicateAttributesProperty");

            Assert.Throws<InvalidOperationException>(() => propertyInfo.Get<UnitTestAttribute>());
        }

        [Test]
        public void TestMultipleDifferentAttributes()
        {
            var propertyInfo = typeof(TestPropertyClass).GetProperty("DifferentAttributesProperty");

            var attr1 = propertyInfo.Get<UnitTestAttribute>();
            var attr2 = propertyInfo.Get<MaxAttribute>();

            Assert.IsNotNull(attr1);
            Assert.IsNotNull(attr2);

            Assert.AreEqual("test2", attr1.Value);
            Assert.AreEqual("test3", attr2.Value);
        }

        [Test]
        public void TestPropertiesOnBaseClass()
        {
            var propertyInfo = typeof(Subclass).GetProperty("OneAttributeProperty");

            var attr1 = propertyInfo.Get<UnitTestAttribute>();

            Assert.IsNotNull(attr1);
        }
    }
}
