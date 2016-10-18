using Moq;
using NsqSharp.Bus;
using NUnit.Framework;

namespace NsqSharp.Tests.Bus
{
    [TestFixture]
    public class CurrentThreadMessageMockableTest
    {
        [Test]
        public void CurrentThreadMessageTest()
        {
            // arrange
            var bus = new Mock<IBus>();
            var currentMessage = new Mock<IMessage>();
            int touchCount = 0;

            currentMessage.Setup(p => p.Touch()).Callback(() => touchCount++);
            bus.SetupGet(p => p.CurrentThreadMessage).Returns(currentMessage.Object);

            // act
            for (int i = 0; i < 2; i++)
            {
                bus.Object.CurrentThreadMessage.Touch();
            }

            // assert
            Assert.AreEqual(2, touchCount);
            currentMessage.Verify(p => p.Touch(), Times.Exactly(2));
        }

        [Test]
        public void CurrentThreadMessageInformationTest()
        {
            // arrange
            var bus = new Mock<IBus>();
            var currentMessage = new Mock<IMessage>();
            var currentMessageInformation = new Mock<ICurrentMessageInformation>();
            int touchCount = 0;

            currentMessage.Setup(p => p.Touch()).Callback(() => touchCount++);
            currentMessageInformation.SetupGet(p => p.Message).Returns(currentMessage.Object);
            bus.Setup(p => p.GetCurrentThreadMessageInformation()).Returns(currentMessageInformation.Object);

            // act
            for (int i = 0; i < 3; i++)
            {
                bus.Object.GetCurrentThreadMessageInformation().Message.Touch();
            }

            // assert
            Assert.AreEqual(3, touchCount);
            currentMessage.Verify(p => p.Touch(), Times.Exactly(3));
        }
    }
}
