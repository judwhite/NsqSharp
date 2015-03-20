using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Logging;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;
using StructureMap;

namespace NsqSharp.Bus.Tests
{
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
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
                    new MessageAuditor(),
                    new MessageTypeToTopicProvider(new Dictionary<Type, string> { 
                        { typeof(TouchTestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelProvider(new Dictionary<Type, string> { 
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
                    }
                ));

                BlockingNoTouchTestHandler.Reset();

                var bus = container.GetInstance<IBus>();

                // publishing registers this nsqd as a producer, causes consumer to connect via lookup service
                // TODO: this test would be faster if the cosumer connected to nsqd directly, but that's not the
                // TODO: pattern to encourage
                bus.Send(new TouchTestMessage { Ignore = true });
                Thread.Sleep(TimeSpan.FromSeconds(6));

                // send our message which will get timed out
                bus.Send(new TouchTestMessage());
                var signaled = BlockingNoTouchTestHandler.Wait(TimeSpan.FromSeconds(10));

                Assert.IsTrue(signaled, "signaled");

                int count = BlockingNoTouchTestHandler.GetCount();
                Assert.AreEqual(2, count);

                // give it a chance the possibly requeue/reprocess again
                Thread.Sleep(10);

                count = BlockingNoTouchTestHandler.GetCount();
                Assert.AreEqual(2, count);

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
                Assert.AreEqual(1, channel.TimeoutCount, "channel.TimeoutCount"); // 1 timeout
                Assert.AreEqual(1, channel.RequeueCount, "channel.RequeueCount"); // 1 requeue
            }
            finally
            {
                BusService.Stop();
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
                    new MessageAuditor(),
                    new MessageTypeToTopicProvider(new Dictionary<Type, string> { 
                        { typeof(TouchTestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelProvider(new Dictionary<Type, string> { 
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
                    }
                ));

                BlockingTouchTestHandler.Reset();

                var bus = container.GetInstance<IBus>();

                // publishing registers this nsqd as a producer, causes consumer to connect via lookup service
                // TODO: this test would be faster if the cosumer connected to nsqd directly, but that's not the
                // TODO: pattern to encourage
                bus.Send(new TouchTestMessage { Ignore = true });
                Thread.Sleep(TimeSpan.FromSeconds(6));

                // send our message which will get timed out
                bus.Send(new TouchTestMessage());
                var signaled = BlockingTouchTestHandler.Wait(TimeSpan.FromSeconds(10));

                Assert.IsTrue(signaled, "signaled");

                int count = BlockingTouchTestHandler.GetCount();
                Assert.AreEqual(1, count);

                // give it a chance the possibly requeue/reprocess again
                Thread.Sleep(10);

                count = BlockingTouchTestHandler.GetCount();
                Assert.AreEqual(1, count);

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
                Assert.AreEqual(0, channel.TimeoutCount, "channel.TimeoutCount"); // 0 timeout
                Assert.AreEqual(0, channel.RequeueCount, "channel.RequeueCount"); // 0 requeue
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        private class MessageAuditor : IMessageAuditor
        {
            public void OnReceived(IBus bus, IMessageInformation info) { }
            public void OnSucceeded(IBus bus, IMessageInformation info) { }
            public void OnFailed(IBus bus, IFailedMessageInformation failedInfo) { }
        }

        public class TouchTestMessage
        {
            public bool Ignore { get; set; }
        }

        public class BlockingNoTouchTestHandler : IHandleMessages<TouchTestMessage>
        {
            private static int _count;
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            public void Handle(TouchTestMessage message)
            {
                if (message.Ignore)
                    return;

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

        public class BlockingTouchTestHandler : IHandleMessages<TouchTestMessage>
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
                if (message.Ignore)
                    return;

                Interlocked.Increment(ref _count);

                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < TimeSpan.FromSeconds(7))
                {
                    Thread.Sleep(50);
                    _bus.CurrentMessage.Touch();
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

        private class MessageTypeToTopicProvider : IMessageTypeToTopicProvider
        {
            private readonly Dictionary<Type, string> _messageTopics;

            public MessageTypeToTopicProvider(IEnumerable<KeyValuePair<Type, string>> messageTopics)
            {
                _messageTopics = new Dictionary<Type, string>();
                foreach (var kvp in messageTopics)
                {
                    _messageTopics.Add(kvp.Key, kvp.Value);
                }
            }

            public string GetTopic(Type messageType)
            {
                return _messageTopics[messageType];
            }
        }

        private class HandlerTypeToChannelProvider : IHandlerTypeToChannelProvider
        {
            private readonly Dictionary<Type, string> _handlerChannels;

            public HandlerTypeToChannelProvider(IEnumerable<KeyValuePair<Type, string>> handlerChannels)
            {
                _handlerChannels = new Dictionary<Type, string>();
                foreach (var kvp in handlerChannels)
                {
                    _handlerChannels.Add(kvp.Key, kvp.Value);
                }
            }

            public string GetChannel(Type handlerType)
            {
                return _handlerChannels[handlerType];
            }

            public IEnumerable<Type> GetHandlerTypes()
            {
                return _handlerChannels.Keys;
            }
        }
    }
}
