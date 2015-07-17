using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Tests.Bus.TestFakes;
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
    public class TouchTest
    {
        [Test]
        public void Given_A_Timeout_Of_3_Seconds_When_Touch_Not_Executed_Then_Nsqd_Should_Requeue()
        {
            string topicName = string.Format("test_touch_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_touch";

            var container = new Container();

            NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            NsqdHttpApi.CreateTopic("http://127.0.0.1:4161", topicName);
            NsqdHttpApi.CreateChannel("http://127.0.0.1:4161", topicName, channelName);

            try
            {
                BusService.Start(new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    new MessageTypeToTopicDictionary(new Dictionary<Type, string> { 
                        { typeof(TouchTestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(BlockingNoTouchTestHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        MessageTimeout = TimeSpan.FromSeconds(3),
                        MaxRequeueDelay = TimeSpan.Zero,
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5)
                    },
                    preCreateTopicsAndChannels: true
                ));

                BlockingNoTouchTestHandler.Reset();

                var bus = container.GetInstance<IBus>();

                // send our message which will get timed out
                bus.Send(new TouchTestMessage());
                var signaled = BlockingNoTouchTestHandler.Wait(TimeSpan.FromSeconds(10));

                Assert.IsTrue(signaled, "signaled");

                int count = BlockingNoTouchTestHandler.GetCount();
                Assert.AreEqual(2, count);

                // give it a chance to possibly requeue/reprocess again
                Thread.Sleep(100);

                count = BlockingNoTouchTestHandler.GetCount();
                Assert.AreEqual(2, count);

                // checks stats from http server
                var stats = NsqdHttpApi.Stats("http://127.0.0.1:4151");
                var topic = stats.Topics.Single(p => p.TopicName == topicName);
                var channel = topic.Channels.Single(p => p.ChannelName == channelName);

                Assert.AreEqual(1, topic.MessageCount, "topic.MessageCount");
                Assert.AreEqual(0, topic.Depth, "topic.Depth");
                Assert.AreEqual(0, topic.BackendDepth, "topic.BackendDepth");

                Assert.AreEqual(1, channel.MessageCount, "channel.MessageCount");
                Assert.AreEqual(0, channel.DeferredCount, "channel.DeferredCount");
                Assert.AreEqual(0, channel.Depth, "channel.Depth");
                Assert.AreEqual(0, channel.BackendDepth, "channel.BackendDepth");
                Assert.AreEqual(0, channel.InFlightCount, "channel.InFlightCount");
                Assert.AreEqual(1, channel.TimeoutCount, "channel.TimeoutCount"); // 1 timeout
                Assert.AreEqual(1, channel.RequeueCount, "channel.RequeueCount"); // 1 requeue
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void Given_A_Timeout_Of_3_Seconds_When_Touch_Executed_Then_Nsqd_Should_Not_Requeue()
        {
            string topicName = string.Format("test_touch_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_touch";

            var container = new Container();

            NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            NsqdHttpApi.CreateTopic("http://127.0.0.1:4161", topicName);
            NsqdHttpApi.CreateChannel("http://127.0.0.1:4161", topicName, channelName);

            try
            {
                BusService.Start(new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    new MessageTypeToTopicDictionary(new Dictionary<Type, string> { 
                        { typeof(TouchTestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(BlockingTouchTestHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        MessageTimeout = TimeSpan.FromSeconds(3),
                        MaxRequeueDelay = TimeSpan.Zero,
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5)
                    },
                    preCreateTopicsAndChannels: true
                ));

                BlockingTouchTestHandler.Reset();

                var bus = container.GetInstance<IBus>();

                // send our message which will get timed out
                bus.Send(new TouchTestMessage());
                var signaled = BlockingTouchTestHandler.Wait(TimeSpan.FromSeconds(10));

                Assert.IsTrue(signaled, "signaled");

                int count = BlockingTouchTestHandler.GetCount();
                Assert.AreEqual(1, count);

                // give it a chance to possibly requeue/reprocess again
                Thread.Sleep(100);

                count = BlockingTouchTestHandler.GetCount();
                Assert.AreEqual(1, count);

                // checks stats from http server
                var stats = NsqdHttpApi.Stats("http://127.0.0.1:4151");
                var topic = stats.Topics.Single(p => p.TopicName == topicName);
                var channel = topic.Channels.Single(p => p.ChannelName == channelName);

                Assert.AreEqual(1, topic.MessageCount, "topic.MessageCount");
                Assert.AreEqual(0, topic.Depth, "topic.Depth");
                Assert.AreEqual(0, topic.BackendDepth, "topic.BackendDepth");

                Assert.AreEqual(1, channel.MessageCount, "channel.MessageCount");
                Assert.AreEqual(0, channel.DeferredCount, "channel.DeferredCount");
                Assert.AreEqual(0, channel.Depth, "channel.Depth");
                Assert.AreEqual(0, channel.BackendDepth, "channel.BackendDepth");
                Assert.AreEqual(0, channel.InFlightCount, "channel.InFlightCount");
                Assert.AreEqual(0, channel.TimeoutCount, "channel.TimeoutCount"); // 0 timeout
                Assert.AreEqual(0, channel.RequeueCount, "channel.RequeueCount"); // 0 requeue
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        private class TouchTestMessage
        {
        }

        private class BlockingNoTouchTestHandler : IHandleMessages<TouchTestMessage>
        {
            private static int _count;
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            public void Handle(TouchTestMessage message)
            {
                int value = Interlocked.Increment(ref _count);
                if (value == 1)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(6));
                }
                else if (value == 2)
                {
                    _wait.Set();
                }
            }

            public static int GetCount()
            {
                return _count;
            }

            public static void Reset()
            {
                Interlocked.Exchange(ref _count, 0);
                _wait.Reset();
            }

            public static bool Wait(TimeSpan timeout)
            {
                return _wait.WaitOne(timeout);
            }
        }

        private class BlockingTouchTestHandler : IHandleMessages<TouchTestMessage>
        {
            private static int _count;
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            private readonly IBus _bus;

            public BlockingTouchTestHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(TouchTestMessage message)
            {
                Interlocked.Increment(ref _count);

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < TimeSpan.FromSeconds(7))
                {
                    Thread.Sleep(50);
                    _bus.CurrentThreadMessage.Touch();
                }

                _wait.Set();
            }

            public static int GetCount()
            {
                return _count;
            }

            public static void Reset()
            {
                Interlocked.Exchange(ref _count, 0);
                _wait.Reset();
            }

            public static bool Wait(TimeSpan timeout)
            {
                return _wait.WaitOne(timeout);
            }
        }
    }
}
