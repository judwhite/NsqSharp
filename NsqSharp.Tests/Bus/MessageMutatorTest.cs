using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Api;
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
    public class MessageMutatorTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static MessageMutatorTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void SetParentIdInMessageMutator()
        {
            string topicName = string.Format("test_message_mutator_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_message_router";

            var container = new Container();

            _nsqLookupdHttpClient.CreateTopic(topicName);
            _nsqLookupdHttpClient.CreateChannel(topicName, channelName);

            try
            {
                BusService.Start(new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    new MessageTypeToTopicDictionary(new Dictionary<Type, string> { 
                        { typeof(MyMutatedMessage), topicName } 
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(MyMutatedMessageHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: new Config
                    {
                        MaxRequeueDelay = TimeSpan.Zero,
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    },
                    preCreateTopicsAndChannels: true,
                    messageMutator: new MessageMutator()
                ));

                var bus = container.GetInstance<IBus>();

                bus.Send(new MyMutatedMessage { Text = "One" });

                var dict = MyMutatedMessageHandler.GetReceived();

                Assert.AreEqual(2, dict.Count, "dict.Count");

                var firstMessage = dict.Keys.First();
                var secondMessage = dict.Keys.Skip(1).Single();

                var expectedParentId = firstMessage.UniqueIdentifier.ToString();

                Console.WriteLine(expectedParentId);
                Console.WriteLine(dict[firstMessage].ParentId);
                Console.WriteLine(dict[firstMessage].Text);
                Console.WriteLine(dict[secondMessage].ParentId);
                Console.WriteLine(dict[secondMessage].Text);

                Assert.AreEqual(null, dict[firstMessage].ParentId, "dict[firstMessage].ParentId");
                Assert.AreEqual("One", dict[firstMessage].Text, "dict[firstMessage].Text");
                Assert.AreEqual(expectedParentId, dict[secondMessage].ParentId, "dict[secondMessage].ParentId");
                Assert.AreEqual("Two", dict[secondMessage].Text, "dict[secondMessage].Text");

                // get stats from http server
                var stats = _nsqdHttpClient.GetStats();

                // assert stats from http server
                var topic = stats.Topics.Single(p => p.TopicName == topicName);
                var channel = topic.Channels.Single(p => p.ChannelName == channelName);

                Assert.AreEqual(2, topic.MessageCount, "topic.MessageCount");
                Assert.AreEqual(0, topic.Depth, "topic.Depth");
                Assert.AreEqual(0, topic.BackendDepth, "topic.BackendDepth");

                Assert.AreEqual(2, channel.MessageCount, "channel.MessageCount");
                Assert.AreEqual(0, channel.DeferredCount, "channel.DeferredCount");
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

        public class MyMutatedMessage : IMessageWithParentId
        {
            public string ParentId { get; set; }
            public string Text { get; set; }
        }

        public interface IMessageWithParentId
        {
            string ParentId { get; set; }
        }

        public class MessageMutator : IMessageMutator
        {
            public T GetMutatedMessage<T>(IBus bus, T sentMessage)
            {
                var currentMessageInfo = bus.GetCurrentThreadMessageInformation();

                IMessageWithParentId messageWithParentId = sentMessage as IMessageWithParentId;
                if (messageWithParentId != null)
                {
                    if (currentMessageInfo != null)
                        messageWithParentId.ParentId = currentMessageInfo.UniqueIdentifier.ToString();
                }

                IMessageWithRouteIndex messageWithRouteIndex = sentMessage as IMessageWithRouteIndex;
                if (messageWithRouteIndex != null && messageWithRouteIndex.RouteIndex == null)
                {
                    if (currentMessageInfo != null)
                    {
                        var currentMessage = currentMessageInfo.DeserializedMessageBody as IMessageWithRouteIndex;
                        if (currentMessage != null)
                            messageWithRouteIndex.RouteIndex = currentMessage.RouteIndex;
                    }
                }

                return sentMessage;
            }
        }

        public class MyMutatedMessageHandler : IHandleMessages<MyMutatedMessage>
        {
            private static readonly Dictionary<ICurrentMessageInformation, MyMutatedMessage> _received =
                new Dictionary<ICurrentMessageInformation, MyMutatedMessage>();
            private static readonly object _receivedLocker = new object();
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            private readonly IBus _bus;

            public MyMutatedMessageHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(MyMutatedMessage message)
            {
                bool done = false;
                lock (_receivedLocker)
                {
                    _received.Add(_bus.GetCurrentThreadMessageInformation(), message);
                    if (_received.Count == 2)
                        done = true;
                    else
                        _bus.Send(new MyMutatedMessage { Text = "Two" });
                }

                if (done)
                    _wait.Set();
            }

            public static Dictionary<ICurrentMessageInformation, MyMutatedMessage> GetReceived()
            {
                _wait.WaitOne(TimeSpan.FromSeconds(10));
                lock (_receivedLocker)
                {
                    return new Dictionary<ICurrentMessageInformation, MyMutatedMessage>(_received);
                }
            }
        }

        [Test]
        public void MutatorPreservesPreviousMessageRoutingProperty()
        {
            var timestamp = DateTime.Now.UnixNano();
            string originalTopicName = string.Format("test_message_router_mutator_{0}", timestamp);
            string topicName1 = string.Format("{0}_1", originalTopicName);
            string topicName2 = string.Format("{0}_2", originalTopicName);
            string childOriginalTopicName = string.Format("test_message_router_mutator_child_{0}", timestamp);
            string childTopicName1 = string.Format("{0}_1", childOriginalTopicName);
            string childTopicName2 = string.Format("{0}_2", childOriginalTopicName);
            const string channelName = "test_message_router_mutator";

            var container = new Container();

            Console.WriteLine(originalTopicName);
            Console.WriteLine(topicName1);
            Console.WriteLine(topicName2);
            Console.WriteLine(childOriginalTopicName);
            Console.WriteLine(childTopicName1);
            Console.WriteLine(childTopicName2);

            _nsqLookupdHttpClient.CreateTopic(originalTopicName);
            _nsqLookupdHttpClient.CreateChannel(originalTopicName, channelName);

            _nsqLookupdHttpClient.CreateTopic(topicName1);
            _nsqLookupdHttpClient.CreateChannel(topicName1, channelName);

            _nsqLookupdHttpClient.CreateTopic(topicName2);
            _nsqLookupdHttpClient.CreateChannel(topicName2, channelName);

            _nsqLookupdHttpClient.CreateTopic(childOriginalTopicName);
            _nsqLookupdHttpClient.CreateChannel(childOriginalTopicName, channelName);

            _nsqLookupdHttpClient.CreateTopic(childTopicName1);
            _nsqLookupdHttpClient.CreateChannel(childTopicName1, channelName);

            _nsqLookupdHttpClient.CreateTopic(childTopicName2);
            _nsqLookupdHttpClient.CreateChannel(childTopicName2, channelName);

            try
            {
                var messageTypeToTopicProvider = new MessageTypeToTopicDictionary(new Dictionary<Type, string> { 
                    { typeof(MyRoutedMessage), originalTopicName },
                    { typeof(MyMutatedRoutedMessage), childOriginalTopicName },
                });

                BusService.Start(new BusConfiguration(
                    new StructureMapObjectBuilder(container),
                    new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                    new MessageAuditorStub(),
                    messageTypeToTopicProvider,
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(MyRoutedMessageHandler), channelName },
                        { typeof(MyMutatedRoutedMessageHandler), channelName },
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: new Config
                    {
                        MaxRequeueDelay = TimeSpan.Zero,
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    },
                    preCreateTopicsAndChannels: true,
                    messageTopicRouter: new MessageTopicRouter(messageTypeToTopicProvider),
                    messageMutator: new MessageMutator()
                ));

                var bus = container.GetInstance<IBus>();

                bus.Send(new MyRoutedMessage { Text = "One" } );
                bus.Send(new MyRoutedMessage { RouteIndex = 1, Text = "Two" });
                bus.Send(new MyRoutedMessage { RouteIndex = 2, Text = "Three" });

                var dict1 = MyRoutedMessageHandler.GetReceived();
                var dict2 = MyMutatedRoutedMessageHandler.GetReceived();

                Assert.AreEqual(3, dict1.Count, "dict.Count");
                Assert.AreEqual(3, dict2.Count, "dict2.Count");

                var firstMessage = dict1.Single(p => p.Value.Text == "One");
                var secondMessage = dict1.Single(p => p.Value.Text == "Two");
                var thirdMessage = dict1.Single(p => p.Value.Text == "Three");

                var childFirstMessage = dict2.Single(p => p.Value.Text == "One Child");
                var childSecondMessage = dict2.Single(p => p.Value.Text == "Two Child");
                var childThirdMessage = dict2.Single(p => p.Value.Text == "Three Child");

                Console.WriteLine(firstMessage.Key.UniqueIdentifier);
                Console.WriteLine(firstMessage.Value.Text);
                Console.WriteLine(secondMessage.Key.UniqueIdentifier);
                Console.WriteLine(secondMessage.Value.Text);
                Console.WriteLine(thirdMessage.Key.UniqueIdentifier);
                Console.WriteLine(thirdMessage.Value.Text);

                Console.WriteLine(childFirstMessage.Value.ParentId);
                Console.WriteLine(childFirstMessage.Value.Text);
                Console.WriteLine(childSecondMessage.Value.ParentId);
                Console.WriteLine(childSecondMessage.Value.Text);
                Console.WriteLine(childThirdMessage.Value.ParentId);
                Console.WriteLine(childThirdMessage.Value.Text);

                Assert.AreEqual("One", firstMessage.Value.Text, "firstMessage.Value.Text");
                Assert.AreEqual("Two", secondMessage.Value.Text, "secondMessage.Value.Text");
                Assert.AreEqual("Three", thirdMessage.Value.Text, "thirdMessage.Value.Text");

                Assert.AreEqual(firstMessage.Key.UniqueIdentifier.ToString(), childFirstMessage.Value.ParentId, 
                    "childFirstMessage.Value.ParentId");
                Assert.AreEqual("One Child", childFirstMessage.Value.Text, "childFirstMessage.Value.Text");
                Assert.AreEqual(secondMessage.Key.UniqueIdentifier.ToString(), childSecondMessage.Value.ParentId, 
                    "childSecondMessage.Value.ParentId");
                Assert.AreEqual("Two Child", childSecondMessage.Value.Text, "childSecondMessage.Value.Text");
                Assert.AreEqual(thirdMessage.Key.UniqueIdentifier.ToString(), childThirdMessage.Value.ParentId, 
                    "childThirdMessage.Value.ParentId");
                Assert.AreEqual("Three Child", childThirdMessage.Value.Text, "childThirdMessage.Value.Text");

                // get stats from http server
                var stats = _nsqdHttpClient.GetStats();

                foreach (var topicName in new[] { 
                    originalTopicName, topicName1, topicName2,
                    childOriginalTopicName, childTopicName1, childTopicName2
                })
                {
                    // assert received message topic/message match expectations
                    IMessageWithRouteIndex receivedMessage;
                    if (dict1.Any(p => p.Key.Topic == topicName))
                        receivedMessage = dict1.Single(p => p.Key.Topic == topicName).Value;
                    else
                        receivedMessage = dict2.Single(p => p.Key.Topic == topicName).Value;

                    int? expectedRouteIndex;
                    if (topicName == topicName1 || topicName == childTopicName1)
                        expectedRouteIndex = 1;
                    else if (topicName == topicName2 || topicName == childTopicName2)
                        expectedRouteIndex = 2;
                    else
                        expectedRouteIndex = null;

                    Assert.AreEqual(expectedRouteIndex, receivedMessage.RouteIndex, "expectedRouteIndex");

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

                _nsqdHttpClient.DeleteTopic(originalTopicName);
                _nsqLookupdHttpClient.DeleteTopic(originalTopicName);

                _nsqdHttpClient.DeleteTopic(topicName1);
                _nsqLookupdHttpClient.DeleteTopic(topicName1);

                _nsqdHttpClient.DeleteTopic(topicName2);
                _nsqLookupdHttpClient.DeleteTopic(topicName2);

                _nsqdHttpClient.DeleteTopic(childOriginalTopicName);
                _nsqLookupdHttpClient.DeleteTopic(childOriginalTopicName);

                _nsqdHttpClient.DeleteTopic(childTopicName1);
                _nsqLookupdHttpClient.DeleteTopic(childTopicName1);

                _nsqdHttpClient.DeleteTopic(childTopicName2);
                _nsqLookupdHttpClient.DeleteTopic(childTopicName2);
            }
        }

        public class MyRoutedMessage : IMessageWithRouteIndex
        {
            public int? RouteIndex { get; set; }
            public string Text { get; set; }
        }

        public class MyMutatedRoutedMessage : IMessageWithRouteIndex, IMessageWithParentId
        {
            public string ParentId { get; set; }
            public int? RouteIndex { get; set; }
            public string Text { get; set; }
        }

        public interface IMessageWithRouteIndex
        {
            int? RouteIndex { get; set; }
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
                    if (myRoutedMessage.RouteIndex == null)
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
                    _bus.Send(new MyMutatedRoutedMessage { Text = message.Text + " Child" });
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

        public class MyMutatedRoutedMessageHandler : IHandleMessages<MyMutatedRoutedMessage>
        {
            private static readonly Dictionary<ICurrentMessageInformation, MyMutatedRoutedMessage> _received =
                new Dictionary<ICurrentMessageInformation, MyMutatedRoutedMessage>();
            private static readonly object _receivedLocker = new object();
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            private readonly IBus _bus;

            public MyMutatedRoutedMessageHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(MyMutatedRoutedMessage message)
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

            public static Dictionary<ICurrentMessageInformation, MyMutatedRoutedMessage> GetReceived()
            {
                _wait.WaitOne(TimeSpan.FromSeconds(10));
                lock (_receivedLocker)
                {
                    return new Dictionary<ICurrentMessageInformation, MyMutatedRoutedMessage>(_received);
                }
            }
        }
    }
}
