using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Tests.Fakes;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;
using StructureMap;

namespace NsqSharp.Bus.Tests
{
#if !RUN_INTEGRATION_TESTS
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
#else
    [TestFixture]
#endif
    public class DeferTest
    {
        [Test]
        public void Given_A_Default_Requeue_Of_90s_When_Requeued_For_3s_Then_Message_Should_Be_Reprocessed_Within_Tolerance()
        {
            string topicName = string.Format("test_defer_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_defer";

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
                    new MessageTypeToTopicProviderFake(new Dictionary<Type, string> { 
                        { typeof(DeferMessage), topicName } 
                    }),
                    new HandlerTypeToChannelProviderFake(new Dictionary<Type, string> { 
                        { typeof(DeferHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5),
                        DefaultRequeueDelay = TimeSpan.FromSeconds(90)
                    }
                ));

                var bus = container.GetInstance<IBus>();

                // publishing registers this nsqd as a producer, causes consumer to connect via lookup service
                // TODO: this test would be faster if the cosumer connected to nsqd directly, but that's not the
                // TODO: pattern to encourage
                bus.Send(new DeferMessage { Ignore = true });
                Thread.Sleep(TimeSpan.FromSeconds(6));

                // send our message which will get timed out
                bus.Send(new DeferMessage { DeferUntil = DateTime.UtcNow.AddSeconds(3) });

                // allow message to process/requeue/process
                Thread.Sleep(TimeSpan.FromSeconds(10));

                var list = DeferHandler.GetReceivedMessages();
                Assert.AreEqual(2, list.Count, "list.Count");

                TimeSpan timeBetweenMessages = list[1].Received - list[0].Received;
                TimeSpan time1Latency = list[0].Received - list[0].OriginalTimestamp;
                TimeSpan time2Latency = list[1].Received - list[0].OriginalTimestamp;
                
                Console.WriteLine(string.Format("Time between messages: {0} (ideal = 3s)", timeBetweenMessages));
                Console.WriteLine(string.Format("Time 1 latency: {0} (ideal = 0s)", time1Latency));
                Console.WriteLine(string.Format("Time 2 latency: {0} (ideal = 3s)", time2Latency));
                
                Assert.GreaterOrEqual(timeBetweenMessages, TimeSpan.FromSeconds(3), "timeBetweenMessages");
                Assert.Less(timeBetweenMessages, TimeSpan.FromSeconds(3.5), "timeBetweenMessages");

                Assert.GreaterOrEqual(time1Latency, TimeSpan.FromSeconds(0), "timeBetweenMessages");
                Assert.Less(time1Latency, TimeSpan.FromSeconds(0.5), "timeBetweenMessages");

                Assert.GreaterOrEqual(time2Latency, TimeSpan.FromSeconds(3), "timeBetweenMessages");
                Assert.Less(time2Latency, TimeSpan.FromSeconds(3.5), "timeBetweenMessages");

                // checks stats from http server
                var stats = NsqdHttpApi.Stats("http://127.0.0.1:4151");
                var topic = stats.Topics.Single(p => p.TopicName == topicName);
                var channel = topic.Channels.Single(p => p.ChannelName == channelName);

                Assert.AreEqual(2, topic.MessageCount, "topic.MessageCount"); // ignored kick-off + test message
                Assert.AreEqual(0, topic.Depth, "topic.Depth");
                Assert.AreEqual(0, topic.BackendDepth, "topic.BackendDepth");

                Assert.AreEqual(2, channel.MessageCount, "channel.MessageCount"); // ignored kick-off + test message
                Assert.AreEqual(0, channel.DeferredCount, "channel.DeferredCount");
                Assert.AreEqual(0, channel.Depth, "channel.Depth");
                Assert.AreEqual(0, channel.BackendDepth, "channel.BackendDepth");
                Assert.AreEqual(0, channel.InFlightCount, "channel.InFlightCount");
                Assert.AreEqual(0, channel.TimeoutCount, "channel.TimeoutCount");
                Assert.AreEqual(1, channel.RequeueCount, "channel.RequeueCount"); // 1 requeue
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        private class DeferMessage
        {
            public bool Ignore { get; set; }
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
                if (message.Ignore)
                    return;

                var delta = message.DeferUntil - DateTime.UtcNow;
                if (delta > TimeSpan.Zero)
                {
                    _bus.CurrentMessage.RequeueWithoutBackoff(delta);
                }

                lock (_queueLocker)
                {
                    var originalTimestamp = _bus.CurrentMessage.Timestamp;
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
