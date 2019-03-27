using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Api;
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
    public class DeferTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static DeferTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void Given_A_Default_Requeue_Of_90s_When_Requeued_For_3s_Then_Message_Should_Be_Reprocessed_Within_Tolerance()
        {
            string topicName = string.Format("test_defer_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_defer";

            var container = new Container();

            _nsqdHttpClient.CreateTopic(topicName);
            _nsqLookupdHttpClient.CreateTopic(topicName);

            BusConfiguration busConfiguration = null;
            try
            {
                busConfiguration = new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    new MessageTypeToTopicDictionary(new Dictionary<Type, string> {
                        { typeof(DeferMessage), topicName }
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> {
                        { typeof(DeferHandler), channelName }
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromMilliseconds(10),
                        DefaultRequeueDelay = TimeSpan.FromSeconds(90)
                    },
                    preCreateTopicsAndChannels: true
                );

                busConfiguration.StartBus();

                var bus = container.GetInstance<IBus>();

                // send our message which will get timed out
                var deferUntil = DateTime.UtcNow.AddSeconds(3);
                bus.Send(new DeferMessage { DeferUntil = deferUntil });

                // allow message to process/requeue/process
                Thread.Sleep(TimeSpan.FromSeconds(10));

                var list = DeferHandler.GetReceivedMessages();
                Assert.AreEqual(2, list.Count, "list.Count");

                TimeSpan timeBetweenMessages = list[1].Received - list[0].Received;
                TimeSpan time1Latency = list[0].Received - list[0].OriginalTimestamp;
                TimeSpan time2Latency = list[1].Received - list[0].OriginalTimestamp;
                TimeSpan deferDelta = list[1].Received - deferUntil;

                Console.WriteLine(string.Format("Time between messages: {0} (ideal < 3s)", timeBetweenMessages));
                Console.WriteLine(string.Format("Time 1 latency: {0} (ideal = 0s)", time1Latency));
                Console.WriteLine(string.Format("Time 2 latency: {0} (ideal < 3s)", time2Latency));
                Console.WriteLine(string.Format("Defer delta: {0} (ideal = 0s)", deferDelta));

                Assert.Less(timeBetweenMessages, TimeSpan.FromSeconds(3.5), "timeBetweenMessages");

                Assert.GreaterOrEqual(time1Latency, TimeSpan.FromSeconds(0), "time1Latency");
                Assert.Less(time1Latency, TimeSpan.FromSeconds(0.5), "time1Latency");

                Assert.Less(time2Latency, TimeSpan.FromSeconds(3.5), "time2Latency");

                Assert.GreaterOrEqual(deferDelta, TimeSpan.FromSeconds(0), "deferDelta");
                Assert.Less(deferDelta, TimeSpan.FromSeconds(0.5), "deferDelta");

                // checks stats from http server
                var stats = _nsqdHttpClient.GetStats();
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
                Assert.AreEqual(0, channel.TimeoutCount, "channel.TimeoutCount");
                Assert.AreEqual(1, channel.RequeueCount, "channel.RequeueCount"); // 1 requeue
            }
            finally
            {
                busConfiguration?.StopBus();
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private class DeferMessage
        {
            public DateTime DeferUntil { get; set; }
        }

        private class MessageInfo
        {
            public DateTime Received { get; set; }
            public DateTime OriginalTimestamp { get; set; }
            public DeferMessage Message { get; set; }
        }

        private class DeferHandler : IHandleMessages<DeferMessage>
        {
            private static readonly List<MessageInfo> _queue = new List<MessageInfo>();
            private static readonly object _queueLocker = new object();

            private readonly IBus _bus;

            public DeferHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(DeferMessage message)
            {
                var delta = message.DeferUntil - DateTime.UtcNow;
                if (delta > TimeSpan.Zero)
                {
                    _bus.CurrentThreadMessage.RequeueWithoutBackoff(delta);
                }

                lock (_queueLocker)
                {
                    var originalTimestamp = _bus.CurrentThreadMessage.Timestamp;
                    _queue.Add(new MessageInfo
                    {
                        Received = DateTime.UtcNow,
                        OriginalTimestamp = originalTimestamp,
                        Message = message
                    });
                }
            }

            public static List<MessageInfo> GetReceivedMessages()
            {
                lock (_queueLocker)
                {
                    return new List<MessageInfo>(_queue);
                }
            }
        }
    }
}
