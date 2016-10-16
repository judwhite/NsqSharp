using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using Newtonsoft.Json;
using NsqSharp.Api;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Logging;
using NsqSharp.Utils;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;
using StructureMap;

namespace NsqSharp.Tests.Bus
{
#if !RUN_INTEGRATION_TESTS
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
#else
    [TestFixture]
#endif
    public class MessageDistributorTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static MessageDistributorTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        private class TestData
        {
            public string TopicPrefix { get; set; }
            public Type HandlerType { get; set; }
            public bool ExpectOnSuccess { get; set; }
            public bool ExpectOnFailed { get; set; }
            public int RequeueCount { get; set; }
            public FailedMessageReason? FailedMessageReason { get; set; }
            public FailedMessageQueueAction? FailedMessageQueueAction { get; set; }
            public ushort? MaxAttempts { get; set; }
        }

        [Test]
        public void TestFailedMessageQueueActionIsFinishIfManuallyFinishedThenExceptionThrown()
        {
            var testData = new TestData
            {
                TopicPrefix = "test_manual_finish_throw",
                HandlerType = typeof(ManualFinishThrowExceptionHandler),
                ExpectOnFailed = true,
                RequeueCount = 0,
                FailedMessageReason = FailedMessageReason.HandlerException,
                FailedMessageQueueAction = FailedMessageQueueAction.Finish
            };

            RunTest(testData);
        }

        [Test]
        public void TestMessageIsSuccessfulIfManuallyFinishedAndNoExceptionThrown()
        {
            var testData = new TestData
            {
                TopicPrefix = "test_manual_finish",
                HandlerType = typeof(ManualFinishHandler),
                ExpectOnSuccess = true,
                RequeueCount = 0
            };

            RunTest(testData);
        }

        [Test]
        public void TestMessageIsRequeuedIfNotManuallyFinishedAndExceptionThrown()
        {
            var testData = new TestData
            {
                TopicPrefix = "test_throw",
                HandlerType = typeof(ThrowExceptionHandler),
                ExpectOnFailed = true,
                RequeueCount = 1,
                FailedMessageReason = FailedMessageReason.HandlerException,
                FailedMessageQueueAction = FailedMessageQueueAction.Requeue
            };

            RunTest(testData);
        }

        [Test]
        public void TestMessageIsSuccessfulWhenHandleFinishesNormally()
        {
            var testData = new TestData
            {
                TopicPrefix = "test_empty_handler",
                HandlerType = typeof(EmptyHandler),
                ExpectOnSuccess = true,
                RequeueCount = 0
            };

            RunTest(testData);
        }

        [Test]
        public void TestMessageFailedReasonIsMaxAttemptsExceededWhenMaxAttemptsIs1AndHandlerThrows()
        {
            var testData = new TestData
            {
                TopicPrefix = "test_max_attempts_exceeded",
                HandlerType = typeof(ThrowExceptionHandler),
                ExpectOnFailed = true,
                RequeueCount = 0,
                FailedMessageReason = FailedMessageReason.MaxAttemptsExceeded,
                FailedMessageQueueAction = FailedMessageQueueAction.Finish,
                MaxAttempts = 1
            };

            RunTest(testData);
        }

        [Test]
        public void TestMessageFailedReasonIsMaxAttemptsExceededWhenMaxAttemptsIs1HandlerManuallyFinishesAndThrows()
        {
            var testData = new TestData
            {
                TopicPrefix = "test_max_attempts_exceeded_manual_finish",
                HandlerType = typeof(ManualFinishThrowExceptionHandler),
                ExpectOnFailed = true,
                RequeueCount = 0,
                FailedMessageReason = FailedMessageReason.MaxAttemptsExceeded,
                FailedMessageQueueAction = FailedMessageQueueAction.Finish,
                MaxAttempts = 1
            };

            RunTest(testData);
        }

        private void RunTest(TestData td)
        {
            string topicName = string.Format("{0}_{1}", td.TopicPrefix, DateTime.Now.UnixNano());
            string channelName = td.TopicPrefix;
            var container = new Container();

            _nsqdHttpClient.CreateTopic(topicName);
            _nsqLookupdHttpClient.CreateTopic(topicName);

            var wg = new WaitGroup();
            wg.Add(1);

            IFailedMessageInformation actualFailedMessageInfo = null;
            IMessageInformation actualSuccessMessageInfo = null;

            var fakeMessageAuditor = new Mock<IMessageAuditor>(MockBehavior.Strict);
            fakeMessageAuditor.Setup(p => p.OnReceived(It.IsAny<IBus>(), It.IsAny<IMessageInformation>()));
            fakeMessageAuditor.Setup(p => p.OnSucceeded(It.IsAny<IBus>(), It.IsAny<IMessageInformation>()))
                              .Callback((IBus bus, IMessageInformation mi) =>
                                        {
                                            if (actualSuccessMessageInfo != null)
                                                throw new Exception("actualSuccessMessageInfo already set");

                                            actualSuccessMessageInfo = mi;
                                            wg.Done();
                                        }
            );

            fakeMessageAuditor.Setup(p => p.OnFailed(It.IsAny<IBus>(), It.IsAny<IFailedMessageInformation>()))
                              .Callback((IBus bus, IFailedMessageInformation fmi) =>
                                        {
                                            if (actualFailedMessageInfo != null)
                                                throw new Exception("actualFailedMessageInfo already set");

                                            actualFailedMessageInfo = fmi;
                                            wg.Done();
                                        }
            );

            try
            {
                var nsqConfig = new Config
                {
                    LookupdPollJitter = 0,
                    LookupdPollInterval = TimeSpan.FromMilliseconds(10),
                    DefaultRequeueDelay = TimeSpan.FromSeconds(90)
                };

                if (td.MaxAttempts != null)
                    nsqConfig.MaxAttempts = td.MaxAttempts.Value;

                var busConfig = new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    fakeMessageAuditor.Object,
                    new MessageTypeToTopicDictionary(new Dictionary<Type, string>
                    {
                        { typeof(StubMessage), topicName }
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string>
                    {
                        { td.HandlerType, channelName }
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: nsqConfig,
                    preCreateTopicsAndChannels: true
                );

                BusService.Start(busConfig);

                var bus = container.GetInstance<IBus>();

                // send the message and wait for the WaitGroup to finish.
                bus.Send(new StubMessage());

                wg.Wait();

                Thread.Sleep(200); // wait for nsqd to process the REQ

                if (td.ExpectOnSuccess)
                    Assert.IsNotNull(actualSuccessMessageInfo, "actualSuccessMessageInfo");
                else
                    Assert.IsNull(actualSuccessMessageInfo, "actualSuccessMessageInfo");

                if (td.ExpectOnFailed)
                {
                    Assert.IsNotNull(actualFailedMessageInfo, "actualFailedMessageInfo");
                    Assert.AreEqual(td.FailedMessageReason, actualFailedMessageInfo.FailedReason, "failedReason");
                    Assert.AreEqual(td.FailedMessageQueueAction, actualFailedMessageInfo.FailedAction, "failedAction");
                }
                else
                {
                    Assert.IsNull(actualFailedMessageInfo, "actualFailedMessageInfo");
                }

                // checks stats from http server
                var stats = _nsqdHttpClient.GetStats();
                var topic = stats.Topics.Single(p => p.TopicName == topicName);
                var channel = topic.Channels.Single(p => p.ChannelName == channelName);

                Assert.AreEqual(1, topic.MessageCount, "topic.MessageCount");
                Assert.AreEqual(0, topic.Depth, "topic.Depth");
                Assert.AreEqual(0, topic.BackendDepth, "topic.BackendDepth");

                Assert.AreEqual(1, channel.MessageCount, "channel.MessageCount");
                // note: until the Requeue Timeout elapses the message is considered Deferred
                Assert.AreEqual(td.RequeueCount, channel.DeferredCount, "channel.DeferredCount");
                Assert.AreEqual(0, channel.Depth, "channel.Depth");
                Assert.AreEqual(0, channel.BackendDepth, "channel.BackendDepth");
                Assert.AreEqual(0, channel.InFlightCount, "channel.InFlightCount");
                Assert.AreEqual(0, channel.TimeoutCount, "channel.TimeoutCount");
                Assert.AreEqual(0, channel.RequeueCount, "channel.RequeueCount");
            }
            finally
            {
                BusService.Stop();
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private class ManualFinishThrowExceptionHandler : IHandleMessages<StubMessage>
        {
            private readonly IBus _bus;

            public ManualFinishThrowExceptionHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(StubMessage message)
            {
                _bus.CurrentThreadMessage.Finish();
                throw new TestException();
            }
        }

        private class ManualFinishHandler : IHandleMessages<StubMessage>
        {
            private readonly IBus _bus;

            public ManualFinishHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(StubMessage message)
            {
                _bus.CurrentThreadMessage.Finish();
            }
        }

        private class ThrowExceptionHandler : IHandleMessages<StubMessage>
        {
            public void Handle(StubMessage message)
            {
                throw new TestException();
            }
        }

        private class EmptyHandler : IHandleMessages<StubMessage>
        {
            public void Handle(StubMessage message)
            {
            }
        }

        private class StubMessage
        {
        }

        private class TestException : Exception
        {
        }
    }
}
