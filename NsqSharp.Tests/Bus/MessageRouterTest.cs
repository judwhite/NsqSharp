using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Bus.Configuration.Providers;
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
    public class MessageRouterTest
    {
        [Test]
        public void RoutingByProperty()
        {
            var timestamp = DateTime.Now.UnixNano();
            string originalTopicName = string.Format("test_message_router_{0}", timestamp);
            string topicName1 = string.Format("{0}_1", originalTopicName);
            string topicName2 = string.Format("{0}_2", originalTopicName);
            const string channelName = "test_message_router";

            var container = new Container();

            NsqdHttpApi.CreateTopic("http://127.0.0.1:4161", originalTopicName);
            NsqdHttpApi.CreateChannel("http://127.0.0.1:4161", originalTopicName, channelName);

            NsqdHttpApi.CreateTopic("http://127.0.0.1:4161", topicName1);
            NsqdHttpApi.CreateChannel("http://127.0.0.1:4161", topicName1, channelName);

            NsqdHttpApi.CreateTopic("http://127.0.0.1:4161", topicName2);
            NsqdHttpApi.CreateChannel("http://127.0.0.1:4161", topicName2, channelName);

            try
            {
                var messageTypeToTopicProvider = new MessageTypeToTopicDictionary(new Dictionary<Type, string> { 
                    { typeof(MyRoutedMessage), originalTopicName } 
                });

                BusService.Start(new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    messageTypeToTopicProvider,
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(MyRoutedMessageHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        MaxRequeueDelay = TimeSpan.Zero,
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    },
                    preCreateTopicsAndChannels: true,
                    messageTopicRouter: new MessageTopicRouter(messageTypeToTopicProvider)
                ));

                var bus = container.GetInstance<IBus>();

                bus.Send(new MyRoutedMessage());
                bus.Send(new MyRoutedMessage { RouteIndex = 1 });
                bus.Send(new MyRoutedMessage { RouteIndex = 2 });

                var dict = MyRoutedMessageHandler.GetReceived();

                Assert.AreEqual(3, dict.Count, "dict.Count");

                // get stats from http server
                var stats = NsqdHttpApi.Stats("http://127.0.0.1:4151");

                foreach (var topicName in new[] { originalTopicName, topicName1, topicName2 })
                {
                    // assert received message topic/message match expectations
                    var receivedMessage = dict.Single(p => p.Key.Topic == topicName);
                    
                    int expectedRouteIndex;
                    if (topicName == topicName1)
                        expectedRouteIndex = 1;
                    else if (topicName == topicName2)
                        expectedRouteIndex = 2;
                    else
                        expectedRouteIndex = 0;

                    Assert.AreEqual(expectedRouteIndex, receivedMessage.Value.RouteIndex, "expectedRouteIndex");

                    // assert stats from http server
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
                    Assert.AreEqual(0, channel.RequeueCount, "channel.RequeueCount");
                }
            }
            finally
            {
                BusService.Stop();

                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4151", originalTopicName);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", originalTopicName);

                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4151", topicName1);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName1);

                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4151", topicName2);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName2);
            }
        }

        public class MyRoutedMessage : IMessageWithRouteIndex
        {
            public int RouteIndex { get; set; }
        }

        public interface IMessageWithRouteIndex
        {
            int RouteIndex { get; }
        }

        public class MessageTopicRouter : IMessageTopicRouter
        {
            private readonly IMessageTypeToTopicProvider _messageTypeToTopicProvider;

            public MessageTopicRouter(IMessageTypeToTopicProvider messageTypeToTopicProvider)
            {
                _messageTypeToTopicProvider = messageTypeToTopicProvider;
            }

            public string GetMessageTopic<T>(IBus bus, string originalTopic, T sentMessage)
            {
                var myRoutedMessage = sentMessage as IMessageWithRouteIndex;
                if (myRoutedMessage != null)
                {
                    if (myRoutedMessage.RouteIndex == 0)
                        return originalTopic;
                    else
                        return string.Format("{0}_{1}", originalTopic, myRoutedMessage.RouteIndex);
                }
                else
                {
                    return originalTopic;
                }
            }

            public string[] GetTopics(Type messageType)
            {
                string originalTopic = _messageTypeToTopicProvider.GetTopic(messageType);

                if (messageType.GetInterfaces().Contains(typeof(IMessageWithRouteIndex)))
                {
                    return new[]
                           {
                               originalTopic,
                               string.Format("{0}_1", originalTopic),
                               string.Format("{0}_2", originalTopic),
                           };
                }
                else
                {
                    return new[] { originalTopic };
                }
            }
        }

        public class MyRoutedMessageHandler : IHandleMessages<MyRoutedMessage>
        {
            private static readonly Dictionary<ICurrentMessageInformation, MyRoutedMessage> _received =
                new Dictionary<ICurrentMessageInformation, MyRoutedMessage>();
            private static readonly object _receivedLocker = new object();
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            private readonly IBus _bus;

            public MyRoutedMessageHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(MyRoutedMessage message)
            {
                bool done = false;
                lock (_receivedLocker)
                {
                    _received.Add(_bus.GetCurrentThreadMessageInformation(), message);
                    if (_received.Count == 3)
                        done = true;
                }

                if (done)
                    _wait.Set();
            }

            public static Dictionary<ICurrentMessageInformation, MyRoutedMessage> GetReceived()
            {
                _wait.WaitOne(TimeSpan.FromSeconds(10));
                lock (_receivedLocker)
                {
                    return new Dictionary<ICurrentMessageInformation, MyRoutedMessage>(_received);
                }
            }
        }
    }
}
