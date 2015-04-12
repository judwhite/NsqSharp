using System;
using System.Collections.Generic;
using System.Text;
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
    public class BusCurrentMessageTest
    {
        [Test]
        public void Given_A_Thread_Not_Handling_A_Message_When_IBus_CurrentMessage_Called_Then_The_Result_Should_Be_Null()
        {
            string topicName = string.Format("test_currentmessage_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_currentmessage";

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
                        { typeof(TestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(TestMessageHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5)
                    }
                ));

                var bus = container.GetInstance<IBus>();

                Assert.IsNull(bus.CurrentMessage, "bus.CurrentMessage");
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void Given_A_Thread_Not_Handling_A_Message_When_GetCurrentMessageInformation_Called_The_Result_Should_Be_Null()
        {
            string topicName = string.Format("test_getcurrentmessageinformation_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_getcurrentmessageinformation";

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
                        { typeof(TestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(TestMessageHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5)
                    }
                ));

                var bus = container.GetInstance<IBus>();

                Assert.IsNull(bus.GetCurrentMessageInformation(), "bus.GetCurrentMessageInformation()");
            }
            finally
            {
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void Given_A_Handler_When_GetCurrentMessageInformation_Called_The_Result_Should_Be_Populated()
        {
            string topicName = string.Format("test_getcurrentmessageinformation_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_getcurrentmessageinformation";

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
                        { typeof(TestMessage), topicName } 
                    }),
                    new HandlerTypeToChannelDictionary(new Dictionary<Type, string> { 
                        { typeof(TestMessageHandler), channelName } 
                    }),
                    defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                    defaultThreadsPerHandler: 1,
                    defaultConsumerNsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(5)
                    },
                    preCreateTopicsAndChannels: true
                ));

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
                BusService.Stop();
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("http://127.0.0.1:4161", topicName);
            }
        }

        private class TestMessage
        {
            public string Message { get; set; }
        }

        private class TestMessageHandler : IHandleMessages<TestMessage>
        {
            private static readonly List<Message> _messages = new List<Message>();
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
                var currentMessageInformation = _bus.GetCurrentMessageInformation();
                var currentMessage = _bus.CurrentMessage;
                lock (_messagesLocker)
                {
                    _messagesInfos.Add(currentMessageInformation);
                    _messages.Add(currentMessage);
                    _wait.Set();
                }
            }

            public static Tuple<Message, ICurrentMessageInformation> GetMessageInfo()
            {
                bool signaled = _wait.WaitOne(TimeSpan.FromSeconds(10));
                if (!signaled)
                    throw new Exception("AutoResetEvent not set");
                Assert.AreEqual(1, _messages.Count, "_messages.Count");
                Assert.AreEqual(1, _messagesInfos.Count, "_messagesInfos.Count");

                return new Tuple<Message, ICurrentMessageInformation>(_messages[0], _messagesInfos[0]);
            }
        }
    }

#if NETFX_3_5
    internal class Tuple<TItem1, TItem2>
    {
        private readonly TItem1 _item1;
        private readonly TItem2 _item2;

        public Tuple(TItem1 item1, TItem2 item2)
        {
            _item1 = item1;
            _item2 = item2;
        }

        public TItem1 Item1 { get { return _item1; } }
        public TItem2 Item2 { get { return _item2; } }
    }
#endif
}
