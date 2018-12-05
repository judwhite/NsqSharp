using System;
using System.Collections.Generic;
using System.Text;
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
    public class BusCurrentMessageTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static BusCurrentMessageTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("http://127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("http://127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void Given_A_Thread_Not_Handling_A_Message_When_IBus_CurrentMessage_Called_Then_The_Result_Should_Be_Null()
        {
            string topicName = string.Format("test_currentmessage_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_currentmessage";

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
                        { typeof(TestMessage), topicName }
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> {
                        { typeof(TestMessageHandler), channelName }
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    }
                );

                busConfiguration.StartBus();

                var bus = container.GetInstance<IBus>();

                Assert.IsNull(bus.CurrentThreadMessage, "bus.CurrentThreadMessage");
            }
            finally
            {
                busConfiguration.StopBus();
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        [Test]
        public void Given_A_Thread_Not_Handling_A_Message_When_GetCurrentMessageInformation_Called_The_Result_Should_Be_Null()
        {
            string topicName = string.Format("test_getcurrentmessageinformation_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_getcurrentmessageinformation";

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
                        { typeof(TestMessage), topicName }
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> {
                        { typeof(TestMessageHandler), channelName }
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    }
                );

                busConfiguration.StartBus();

                var bus = container.GetInstance<IBus>();

                Assert.IsNull(bus.GetCurrentThreadMessageInformation(), "bus.GetCurrentThreadMessageInformation()");
            }
            finally
            {
                busConfiguration.StopBus();
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        [Test]
        public void Given_A_Handler_When_GetCurrentMessageInformation_Called_The_Result_Should_Be_Populated()
        {
            string topicName = string.Format("test_getcurrentmessageinformation_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_getcurrentmessageinformation";

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
                        { typeof(TestMessage), topicName }
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> {
                        { typeof(TestMessageHandler), channelName }
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    nsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    },
                    preCreateTopicsAndChannels: true
                );

                busConfiguration.StartBus();

                var bus = container.GetInstance<IBus>();

                var testMessage = new TestMessage { Message = "Hello!" };
                bus.Send(testMessage);

                var json = JsonConvert.SerializeObject(testMessage);
                var jsonBytes = Encoding.UTF8.GetBytes(json);

                var tuple = TestMessageHandler.GetMessageInfo();
                var message = tuple.Item1;
                var messageInformation = tuple.Item2;

                Assert.IsNotNull(message, "message");
                Assert.IsNotNull(messageInformation, "messageInformation");

                Assert.AreSame(message, messageInformation.Message);

                Assert.IsNotNull(message.Body, "message.Body");
                Assert.AreEqual(jsonBytes, message.Body, "message.Body");

                Assert.AreEqual(topicName, messageInformation.Topic, "messageInformation.Topic");
                Assert.AreEqual(channelName, messageInformation.Channel, "messageInformation.Channel");
                Assert.AreNotEqual(Guid.Empty, messageInformation.UniqueIdentifier, "messageInformation.UniqueIdentifier");
            }
            finally
            {
                busConfiguration.StopBus();
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private class TestMessage
        {
            public string Message { get; set; }
        }

        private class TestMessageHandler : IHandleMessages<TestMessage>
        {
            private static readonly List<IMessage> _messages = new List<IMessage>();
            private static readonly List<ICurrentMessageInformation> _messagesInfos = new List<ICurrentMessageInformation>();
            private static readonly object _messagesLocker = new object();
            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            private readonly IBus _bus;

            public TestMessageHandler(IBus bus)
            {
                _bus = bus;
            }

            public void Handle(TestMessage message)
            {
                var currentMessageInformation = _bus.GetCurrentThreadMessageInformation();
                var currentMessage = _bus.CurrentThreadMessage;
                lock (_messagesLocker)
                {
                    _messagesInfos.Add(currentMessageInformation);
                    _messages.Add(currentMessage);
                    _wait.Set();
                }
            }

            public static Tuple<IMessage, ICurrentMessageInformation> GetMessageInfo()
            {
                bool signaled = _wait.WaitOne(TimeSpan.FromSeconds(10));
                if (!signaled)
                    throw new Exception("AutoResetEvent not set");
                Assert.AreEqual(1, _messages.Count, "_messages.Count");
                Assert.AreEqual(1, _messagesInfos.Count, "_messagesInfos.Count");

                return new Tuple<IMessage, ICurrentMessageInformation>(_messages[0], _messagesInfos[0]);
            }
        }
    }
}
