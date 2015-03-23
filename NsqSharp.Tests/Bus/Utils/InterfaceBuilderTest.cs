using System;
using NsqSharp.Bus.Utils;
using NUnit.Framework;

namespace NsqSharp.Tests.Bus.Utils
{
    [TestFixture]
    public class InterfaceBuilderTest
    {
        [Test]
        public void MustBeInterface()
        {
            Assert.Throws<ArgumentException>(() => InterfaceBuilder.CreateObject<Details>());
        }

        [Test]
        public void BaseInterfaceWithClassProperty()
        {
            // Arrange
            var expectedTimestamp = new DateTime(2015, 2, 22);
            const int expectedCount = 27;
            const string expectedMessage = "Happy Birthday";

            // Act
            IBaseInterface baseInterface = InterfaceBuilder.CreateObject<IBaseInterface>();

            baseInterface.Details = new Details { Timestamp = expectedTimestamp, Count = expectedCount };
            baseInterface.Message = expectedMessage;

            // Assert
            Assert.AreEqual(expectedTimestamp, baseInterface.Details.Timestamp, "Details.Timestamp");
            Assert.AreEqual(expectedCount, baseInterface.Details.Count, "Details.Count");
            Assert.AreEqual(expectedMessage, baseInterface.Message, "Message");
        }

        [Test]
        public void SubInterface()
        {
            // Arrange
            var expectedTimestamp = new DateTime(2015, 2, 22);
            const int expectedCount = 27;
            const string expectedMessage = "Happy Birthday";
            var expectedId = Guid.NewGuid();
            
            // Act
            ISubInterface subInterface = InterfaceBuilder.CreateObject<ISubInterface>();

            subInterface.Details = new Details { Timestamp = expectedTimestamp, Count = expectedCount };
            subInterface.Message = expectedMessage;
            subInterface.Id = expectedId;

            // Assert
            Assert.AreEqual(expectedTimestamp, subInterface.Details.Timestamp, "Details.Timestamp");
            Assert.AreEqual(expectedCount, subInterface.Details.Count, "Details.Count");
            Assert.AreEqual(expectedMessage, subInterface.Message, "Message");
            Assert.AreEqual(expectedId, subInterface.Id, "Id");
        }

        [Test]
        public void DoubleSubInterface()
        {
            // Arrange
            var expectedTimestamp = new DateTime(2015, 2, 22);
            const int expectedCount = 27;
            const string expectedMessage = "Happy Birthday";
            var expectedId = Guid.NewGuid();
            const double expectedPi = Math.PI;

            // Act
            IDoubleSubInterface obj = InterfaceBuilder.CreateObject<IDoubleSubInterface>();

            obj.Details = new Details { Timestamp = expectedTimestamp, Count = expectedCount };
            obj.Message = expectedMessage;
            obj.Id = expectedId;
            obj.Pi = expectedPi;

            // Assert
            Assert.AreEqual(expectedTimestamp, obj.Details.Timestamp, "Details.Timestamp");
            Assert.AreEqual(expectedCount, obj.Details.Count, "Details.Count");
            Assert.AreEqual(expectedMessage, obj.Message, "Message");
            Assert.AreEqual(expectedId, obj.Id, "Id");
            Assert.AreEqual(expectedPi, obj.Pi, "Pi");
        }

        public class Details
        {
            public DateTime? Timestamp { get; set; }
            public int Count { get; set; }
        }

        public interface IBaseInterface
        {
            string Message { get; set; }
            Details Details { get; set; }
        }

        public interface ISubInterface : IBaseInterface
        {
            Guid Id { get; set; }
        }

        public interface IDoubleSubInterface : ISubInterface
        {
            double Pi { get; set; }
        }
    }
}
