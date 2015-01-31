using System;
using NsqSharp.Go;
using NUnit.Framework;

namespace NsqSharp.Tests.Go
{
    [TestFixture]
    public class SliceTest
    {
        [Test]
        public void TestStringEquality()
        {
            Slice<char> x = new Slice<char>("abcdef");
            string y = "abcdef";

            Assert.IsTrue(x == y);
        }

        [Test]
        public void TestStringEqualityAfterSlice()
        {
            Slice<char> x = new Slice<char>("abcdef");
            x = x.Slc(0, 3);
            string y = "abc";

            Assert.IsTrue(x == y);
        }

        [Test]
        public void TestStringEqualityAfterMidSlice()
        {
            Slice<char> x = new Slice<char>("abcdef");
            x = x.Slc(1, 4);
            string y = "bcd";

            Assert.IsTrue(x == y);
        }

        [Test]
        public void TestStringTypeEqualityMismatch()
        {
            Slice<int> x = new Slice<int>(new[] { 1, 2, 3 });
            string y = "abc";

            Assert.IsFalse(x == y);
        }

        [Test]
        public void TestNullStringEquality()
        {
            Slice<char> x = null;
            string y = null;

            Assert.IsTrue(x == y);
        }

        [Test]
        public void TestRightNullStringInequality()
        {
            Slice<char> x = new Slice<char>("abc");
            string y = null;

            Assert.IsTrue(x != y);
        }

        [Test]
        public void TestLeftNullSliceInequality()
        {
            Slice<char> x = null;
            string y = "abc";

            Assert.IsTrue(x != y);
        }

        [Test]
        public void TestToStringOnSliceChar()
        {
            Slice<char> x = new Slice<char>("hello world");
            var actual = x.Slc(2, 8).ToString();

            Assert.AreEqual("llo wo", actual);
        }

        [Test]
        public void TestToStringOnSliceInt()
        {
            Slice<int> x = new Slice<int>(new[] { 1, 2, 3, 4 });
            string actual = x.ToString();

            Assert.AreEqual("[ 1 2 3 4 ]", actual);
        }

        [Test]
        public void TestSlcHasNoSideEffects()
        {
            Slice<char> x = new Slice<char>("hello world");
            x.Slc(2, 8);

            Assert.AreEqual("hello world".Length, x.Len());
            Assert.AreEqual("hello world", x.ToString());
        }

        [Test]
        public void TestSliceStartLessThan0()
        {
            Slice<char> x = new Slice<char>("test");

            Assert.Throws<ArgumentOutOfRangeException>(() => x.Slc(-1));
        }

        [Test]
        public void TestSliceStartGreaterThanLen()
        {
            Slice<char> x = new Slice<char>("test");

            Assert.Throws<ArgumentOutOfRangeException>(() => x.Slc(5));
        }

        [Test]
        public void TestSliceStartGreaterThanEnd()
        {
            Slice<char> x = new Slice<char>("test");

            Assert.Throws<ArgumentOutOfRangeException>(() => x.Slc(2, 1));
        }

        [Test]
        public void TestSliceEndGreaterThanLen()
        {
            Slice<char> x = new Slice<char>("test");

            Assert.Throws<ArgumentOutOfRangeException>(() => x.Slc(2, 5));
        }

        [Test]
        public void TestSliceEndGreaterThanLenAfterReslice()
        {
            Slice<char> x = new Slice<char>("test");

            x = x.Slc(0, 2);

            Assert.Throws<ArgumentOutOfRangeException>(() => x.Slc(0, 3));
        }

        [Test]
        public void TestSliceAtZero()
        {
            Slice<char> x = new Slice<char>("test!");
            x = x.Slc(0, 1);

            Assert.AreEqual("t", x.ToString());
        }

        [Test]
        public void TestSliceAtEnd()
        {
            Slice<char> x = new Slice<char>("test!");
            x = x.Slc(x.Len() - 1);

            Assert.AreEqual("!", x.ToString());
        }

        [Test]
        public void TestEmptySliceAtZero()
        {
            Slice<char> x = new Slice<char>("test!");
            x = x.Slc(0, 0);

            Assert.AreEqual("", x.ToString());
        }

        [Test]
        public void TestEmptySliceAtEnd()
        {
            Slice<char> x = new Slice<char>("test!");
            x = x.Slc(x.Len());

            Assert.AreEqual("", x.ToString());
        }

        [Test]
        public void TestEmptySliceAtMid()
        {
            Slice<int> x = new Slice<int>(new[] { 1, 2, 3, 4 });
            x = x.Slc(1, 1);

            Assert.IsEmpty(x.ToArray());
        }

        [Test]
        public void TestStringConstructorThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Slice<char>((string)null));
        }

        [Test]
        public void TestStringConstructorThrowsOnTypeMistmatch()
        {
            Assert.Throws<Exception>(() => new Slice<int>("nope"));
        }

        [Test]
        public void TestArrayConstructorThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new Slice<int>((int[])null));
        }
    }
}
