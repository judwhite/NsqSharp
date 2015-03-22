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
    [TestFixture]
    public class MultiImplementIHandleMessagesTest
    {
#if !RUN_INTEGRATION_TESTS
        [Ignore("NSQD Integration Test")]
#endif
        [Test]
        public void Given_Two_Queues_In_One_Process_When_One_Queue_Is_Long_Then_It_Should_Not_Block_Other_Queues()
        {
            string highPriorityTopicName = string.Format("test_high_priority_{0}", DateTime.Now.UnixNano());
            const string highPriorityChannelName = "test_high_priority";
            string lowPriorityTopicName = string.Format("test_low_priority_{0}", DateTime.Now.UnixNano());
            const string lowPriorityChannelName = "test_low_priority";

            var container = new Container();
            container.Configure(x =>
                                {
                                    x.For<IHandleMessages<HighPriority<UpdateMessage>>>().Use<UpdateHandler>();
                                    x.For<IHandleMessages<LowPriority<UpdateMessage>>>().Use<UpdateHandler>();
                                });

            try
            {
                BusService.Start(new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    new MessageTypeToTopicProviderFake(new Dictionary<Type, string> { 
                        { typeof(HighPriority<UpdateMessage>), highPriorityTopicName },
                        { typeof(LowPriority<UpdateMessage>), lowPriorityTopicName }
                    }),
                    new HandlerTypeToChannelProviderFake(new Dictionary<Type, string> { 
                        { typeof(IHandleMessages<HighPriority<UpdateMessage>>), highPriorityChannelName },
                        { typeof(IHandleMessages<LowPriority<UpdateMessage>>), lowPriorityChannelName }
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 4,
                    defaultConsumerNsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5),
                    }
                ));

                var bus = container.GetInstance<IBus>();

                var lowPriorityMessage = UpdateMessage.LowPriority(p => p.Id = "low");
                var highPriorityMessage = UpdateMessage.HighPriority(p => p.Id = "high");
                bus.Send(highPriorityMessage);
                bus.Send(lowPriorityMessage);

                Thread.Sleep(TimeSpan.FromSeconds(6));

                UpdateHandler.Reset();

                // publishing registers this nsqd as a producer, causes consumer to connect via lookup service
                // TODO: this test would be faster if the cosumer connected to nsqd directly, but that's not the
                // TODO: pattern to encourage

                // send 1000 low priority message
                var lowPriorityMessages = new List<LowPriority<UpdateMessage>>();
                for (int i = 0; i < 1000; i++)
                {
                    lowPriorityMessages.Add(lowPriorityMessage);
                }
                bus.SendMulti(lowPriorityMessages);

                // let some go through
                Thread.Sleep(40);
                int lowCountAtSendHigh = UpdateHandler.GetCount();

                // send 1 high priority message
                bus.Send(highPriorityMessage);

                // wait for nsqlookupd cycle / consumer + processing time
                var list = UpdateHandler.GetList();
                int highIndex = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Id == "high")
                    {
                        highIndex = i;
                        break;
                    }
                }

                Console.WriteLine("lowCountAtSendHigh: {0}", lowCountAtSendHigh);
                Console.WriteLine("highIndex: {0}", highIndex);
                Console.WriteLine("total: {0}", list.Count);

                Assert.Less(lowCountAtSendHigh, 800, "lowCountAtSendHigh");
                Assert.Greater(lowCountAtSendHigh, 100, "lowCountAtSendHigh");
                Assert.Less(highIndex, lowCountAtSendHigh + 50, "highIndex");

                // checks stats from http server
                var stats = NsqdHttpApi.Stats("http://127.0.0.1:4151");

                // assert high priority stats
                var highTopic = stats.Topics.Single(p => p.TopicName == highPriorityTopicName);
                var highChannel = highTopic.Channels.Single(p => p.ChannelName == highPriorityChannelName);

                Assert.AreEqual(2, highTopic.MessageCount, "highTopic.MessageCount"); // kickoff + 1 message
                Assert.AreEqual(0, highTopic.Depth, "highTopic.Depth");
                Assert.AreEqual(0, highTopic.BackendDepth, "highTopic.BackendDepth");

                Assert.AreEqual(2, highChannel.MessageCount, "highChannel.MessageCount"); // kickoff + 1 message
                Assert.AreEqual(0, highChannel.DeferredCount, "highChannel.DeferredCount");
                Assert.AreEqual(0, highChannel.Depth, "highChannel.Depth");
                Assert.AreEqual(0, highChannel.BackendDepth, "highChannel.BackendDepth");
                Assert.AreEqual(0, highChannel.InFlightCount, "highChannel.InFlightCount");
                Assert.AreEqual(0, highChannel.TimeoutCount, "highChannel.TimeoutCount");
                Assert.AreEqual(0, highChannel.RequeueCount, "highChannel.RequeueCount");

                // assert low priority stats
                var lowTopic = stats.Topics.Single(p => p.TopicName == lowPriorityTopicName);
                var lowChannel = lowTopic.Channels.Single(p => p.ChannelName == lowPriorityChannelName);

                Assert.AreEqual(1001, lowTopic.MessageCount, "lowTopic.MessageCount"); // kickoff + 1000 messages
                Assert.AreEqual(0, lowTopic.Depth, "lowTopic.Depth");
                Assert.AreEqual(0, lowTopic.BackendDepth, "lowTopic.BackendDepth");

                Assert.AreEqual(1001, lowChannel.MessageCount, "lowChannel.MessageCount"); // kickoff + 1000 messages
                Assert.AreEqual(0, lowChannel.DeferredCount, "lowChannel.DeferredCount");
                Assert.AreEqual(0, lowChannel.Depth, "lowChannel.Depth");
                Assert.AreEqual(0, lowChannel.BackendDepth, "lowChannel.BackendDepth");
                Assert.AreEqual(0, lowChannel.InFlightCount, "lowChannel.InFlightCount");
                Assert.AreEqual(0, lowChannel.TimeoutCount, "lowChannel.TimeoutCount");
                Assert.AreEqual(0, lowChannel.RequeueCount, "lowChannel.RequeueCount");
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", highPriorityTopicName);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", lowPriorityTopicName);
            }
        }

#if !RUN_INTEGRATION_TESTS
        [Ignore("NSQD Integration Test")]
#endif
        [Test]
        public void Given_A_Handler_Implementing_Multiple_IHandleMessages_When_Bus_Starts_Then_An_Exception_Should_Be_Thrown()
        {
            // TODO: Turn off /ping on bus start for unit tests

            string highPriorityTopicName = string.Format("test_high_priority_{0}", DateTime.Now.UnixNano());
            string lowPriorityTopicName = string.Format("test_low_priority_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_channel";

            var container = new Container();

            // UpdateHandler implements IHandleMessages multiple times

            Assert.Throws<HandlerConfigurationException>(() => BusService.Start(new BusConfiguration(
                new StructureMapObjectBuilder(container),
                new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                new MessageAuditorStub(),
                new MessageTypeToTopicProviderFake(new Dictionary<Type, string> { 
                        { typeof(HighPriority<UpdateMessage>), highPriorityTopicName },
                        { typeof(LowPriority<UpdateMessage>), lowPriorityTopicName }
                    }),
                new HandlerTypeToChannelProviderFake(new Dictionary<Type, string> { 
                        { typeof(UpdateHandler), channelName },
                    }),
                defaultNsqLookupdHttpEndpoints: new[] { "0.0.0.0:0" },
                defaultThreadsPerHandler: 4,
                defaultConsumerNsqConfig: new Config
                {
                    LookupdPollJitter = 0,
                    LookupdPollInterval = TimeSpan.FromSeconds(5),
                }
            )));

            // the safe way (explicit channel names for each IHandleMessages<>)

            container.Configure(x =>
            {
                x.For<IHandleMessages<HighPriority<UpdateMessage>>>().Use<UpdateHandler>();
                x.For<IHandleMessages<LowPriority<UpdateMessage>>>().Use<UpdateHandler>();
            });

            BusService.Start(new BusConfiguration(
                new StructureMapObjectBuilder(container),
                new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                new MessageAuditorStub(),
                new MessageTypeToTopicProviderFake(new Dictionary<Type, string> { 
                        { typeof(HighPriority<UpdateMessage>), highPriorityTopicName },
                        { typeof(LowPriority<UpdateMessage>), lowPriorityTopicName }
                    }),
                new HandlerTypeToChannelProviderFake(new Dictionary<Type, string> { 
                        { typeof(IHandleMessages<HighPriority<UpdateMessage>>), channelName },
                        { typeof(IHandleMessages<LowPriority<UpdateMessage>>), channelName }
                    }),
                defaultNsqLookupdHttpEndpoints: new[] { "0.0.0.0:0" },
                defaultThreadsPerHandler: 4,
                defaultConsumerNsqConfig: new Config
                {
                    LookupdPollJitter = 0,
                    LookupdPollInterval = TimeSpan.FromSeconds(5),
                }
            ));
        }

        private class HighPriority<T>
        {
            public T Value { get; set; }
        }

        private class LowPriority<T>
        {
            public T Value { get; set; }
        }

        private class UpdateMessage
        {
            public static HighPriority<UpdateMessage> HighPriority(Action<UpdateMessage> setter)
            {
                var message = new UpdateMessage();
                setter(message);
                return new HighPriority<UpdateMessage> { Value = message };
            }

            public static LowPriority<UpdateMessage> LowPriority(Action<UpdateMessage> setter)
            {
                var message = new UpdateMessage();
                setter(message);
                return new LowPriority<UpdateMessage> { Value = message };
            }

            private UpdateMessage()
            {
            }

            public string Id { get; set; }
            public DateTime Received { get; set; }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class UpdateHandler
            : IHandleMessages<HighPriority<UpdateMessage>>,
              IHandleMessages<LowPriority<UpdateMessage>>
        {
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);
            private static readonly List<UpdateMessage> _list = new List<UpdateMessage>();
            private static readonly object _listLocker = new object();
            private static int _count;

            public void Handle(HighPriority<UpdateMessage> message)
            {
                if (message == null)
                    throw new ArgumentNullException("message");

                Handle(message.Value);
            }

            public void Handle(LowPriority<UpdateMessage> message)
            {
                if (message == null)
                    throw new ArgumentNullException("message");

                Handle(message.Value);
            }

            private void Handle(UpdateMessage message)
            {
                if (message == null)
                    throw new ArgumentNullException("message");

                message.Received = DateTime.UtcNow;
                lock (_listLocker)
                {
                    _list.Add(message);
                }

                if (Interlocked.Increment(ref _count) == 1001)
                {
                    _wait.Set();
                }
            }

            public static void Reset()
            {
                lock (_listLocker)
                {
                    _count = 0;
                    _list.Clear();
                }
            }

            public static int GetCount()
            {
                return _count;
            }

            public static List<UpdateMessage> GetList()
            {
                _wait.WaitOne(TimeSpan.FromSeconds(15));
                return _list.OrderBy(p => p.Received).ToList();
            }
        }
    }
}
