using NsqSharp.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Extensions
{
    [TestFixture]
    public class IntExtensionsTests
    {
        [Test]
        public void UInt32Test()
        {
            // Arrange
            const uint original = 0xE67F23F6;
            const uint expected = 0xF6237FE6;

            // Act
            uint actual = original.AsBigEndian();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Int32Test()
        {
            // Arrange
            const uint original = 0xE67F23F6;
            const uint expected = 0xF6237FE6;

            // Act
            uint actual;
            unchecked
            {
                actual = ((int)original).AsBigEndian();
            }

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UInt16Test()
        {
            // Arrange
            const ushort original = 0xE6F7;
            const ushort expected = 0xF7E6;

            // Act
            ushort actual = original.AsBigEndian();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UInt64Test()
        {
            // Arrange
            const ulong original = 0xE67F2346DEADBEEF;
            const ulong expected = 0xEFBEADDE46237FE6;

            // Act
            ulong actual = original.AsBigEndian();

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
