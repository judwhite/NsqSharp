using System;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Utils.Extensions
{
    [TestFixture]
    public class ObjectExtensionsTest
    {
        [Test]
        public void CoerceNullReturnsNull()
        {
            int? test1 = ((object)null).Coerce<int?>();
            double? test2 = ((object)null).Coerce<double?>();
            decimal? test3 = ((object)null).Coerce<decimal?>();
            string test4 = ((object)null).Coerce<string>();

            Assert.IsNull(test1);
            Assert.IsNull(test2);
            Assert.IsNull(test3);
            Assert.IsNull(test4);
        }

        [Test]
        public void CoerceSameTypeReturns()
        {
            const decimal expected = 123.456m;
            decimal actual = expected.Coerce<decimal>();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CoerceUnsupportedThrowsException()
        {
            Assert.Throws<Exception>(() => (new ObjectExtensionsTest()).Coerce<object>());
        }

        [Test]
        public void CoerceString1ToBool()
        {
            bool result = "1".Coerce<bool>();
            Assert.AreEqual(true, result);
        }

        [Test]
        public void CoerceStringTrueToBool()
        {
            bool result = "true".Coerce<bool>();
            Assert.AreEqual(true, result);
            result = "True".Coerce<bool>();
            Assert.AreEqual(true, result);
            result = "tRue".Coerce<bool>();
            Assert.AreEqual(true, result);
        }

        [Test]
        public void CoerceInt1ToBool()
        {
            bool result = 1.Coerce<bool>();
            Assert.AreEqual(true, result);
        }

        [Test]
        public void CoerceString0ToBool()
        {
            bool result = "0".Coerce<bool>();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void CoerceStringFalseToBool()
        {
            bool result = "false".Coerce<bool>();
            Assert.AreEqual(false, result);
            result = "False".Coerce<bool>();
            Assert.AreEqual(false, result);
            result = "fAlse".Coerce<bool>();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void CoerceInt0ToBool()
        {
            bool result = 0.Coerce<bool>();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void CoerceIntToUnsignedShort()
        {
            ushort result = 0.Coerce<ushort>();
            Assert.AreEqual(0, result);
            result = 123.Coerce<ushort>();
            Assert.AreEqual(123, result);
            result = 65535.Coerce<ushort>();
            Assert.AreEqual(65535, result);
        }

        [Test]
        public void CoerceStringToUnsignedShort()
        {
            ushort result = "0".Coerce<ushort>();
            Assert.AreEqual(0, result);
            result = "123".Coerce<ushort>();
            Assert.AreEqual(123, result);
            result = "65535".Coerce<ushort>();
            Assert.AreEqual(65535, result);
        }

        [Test]
        public void CoerceStringToInt()
        {
            int result = "0".Coerce<int>();
            Assert.AreEqual(0, result);
            result = "123".Coerce<int>();
            Assert.AreEqual(123, result);
            result = "-123".Coerce<int>();
            Assert.AreEqual(-123, result);
            result = int.MaxValue.ToString().Coerce<int>();
            Assert.AreEqual(int.MaxValue, result);
            result = int.MinValue.ToString().Coerce<int>();
            Assert.AreEqual(int.MinValue, result);
        }

        [Test]
        public void CoerceStringToLong()
        {
            long result = "0".Coerce<long>();
            Assert.AreEqual(0, result);
            result = "123".Coerce<long>();
            Assert.AreEqual(123, result);
            result = "-123".Coerce<long>();
            Assert.AreEqual(-123, result);
            result = long.MaxValue.ToString().Coerce<long>();
            Assert.AreEqual(long.MaxValue, result);
            result = long.MinValue.ToString().Coerce<long>();
            Assert.AreEqual(long.MinValue, result);
        }

        [Test]
        public void CoerceIntToLong()
        {
            long result = 0.Coerce<long>();
            Assert.AreEqual(0, result);
            result = 123.Coerce<long>();
            Assert.AreEqual(123, result);
            result = -123.Coerce<long>();
            Assert.AreEqual(-123, result);
            result = int.MaxValue.Coerce<long>();
            Assert.AreEqual(int.MaxValue, result);
            result = int.MinValue.Coerce<long>();
            Assert.AreEqual(int.MinValue, result);
        }

        [Test]
        public void CoerceIntToDouble()
        {
            double result = 0.Coerce<double>();
            Assert.AreEqual(0, result);
            result = 123.Coerce<double>();
            Assert.AreEqual(123, result);
            result = -123.Coerce<double>();
            Assert.AreEqual(-123, result);
            result = int.MaxValue.Coerce<double>();
            Assert.AreEqual(int.MaxValue, result);
            result = int.MinValue.Coerce<double>();
            Assert.AreEqual(int.MinValue, result);
        }

        [Test]
        public void CoerceStringToDouble()
        {
            double result = "0".Coerce<double>();
            Assert.AreEqual(0, result);
            result = "123.456".Coerce<double>();
            Assert.AreEqual(123.456, result);
            result = "-123.456".Coerce<double>();
            Assert.AreEqual(-123.456, result);
            result = int.MaxValue.Coerce<double>();
            Assert.AreEqual(int.MaxValue, result);
            result = int.MinValue.Coerce<double>();
            Assert.AreEqual(int.MinValue, result);
        }

        [Test]
        public void CoerceStringDurationToTimeSpan()
        {
            TimeSpan a = "12s".Coerce<TimeSpan>();
            TimeSpan b = "-12s".Coerce<TimeSpan>();

            Assert.AreEqual(TimeSpan.FromSeconds(12), a);
            Assert.AreEqual(TimeSpan.FromSeconds(-12), b);
        }

        [Test]
        public void CoerceStringMsToTimeSpan()
        {
            TimeSpan a = "12".Coerce<TimeSpan>();
            TimeSpan b = "-12".Coerce<TimeSpan>();

            Assert.AreEqual(TimeSpan.FromMilliseconds(12), a);
            Assert.AreEqual(TimeSpan.FromMilliseconds(-12), b);
        }

        [Test]
        public void CoerceIntMsToTimeSpan()
        {
            TimeSpan a = 12.Coerce<TimeSpan>();
            TimeSpan b = -12.Coerce<TimeSpan>();

            Assert.AreEqual(TimeSpan.FromMilliseconds(12), a);
            Assert.AreEqual(TimeSpan.FromMilliseconds(-12), b);
        }

        [Test]
        public void CoerceLongMsToTimeSpan()
        {
            TimeSpan a = 12L.Coerce<TimeSpan>();
            TimeSpan b = -12L.Coerce<TimeSpan>();

            Assert.AreEqual(TimeSpan.FromMilliseconds(12), a);
            Assert.AreEqual(TimeSpan.FromMilliseconds(-12), b);
        }

        [Test]
        public void CoerceUnsignedLongMsToTimeSpan()
        {
            TimeSpan a = 12UL.Coerce<TimeSpan>();
            TimeSpan b = -12UL.Coerce<TimeSpan>();

            Assert.AreEqual(TimeSpan.FromMilliseconds(12), a);
            Assert.AreEqual(TimeSpan.FromMilliseconds(-12), b);
        }
    }
}
