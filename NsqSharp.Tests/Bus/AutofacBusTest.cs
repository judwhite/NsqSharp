using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Autofac;
using Newtonsoft.Json;
using NsqSharp.Api;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Tests.Bus.TestFakes;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Bus
{
#if !RUN_INTEGRATION_TESTS
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
#else
    [TestFixture]
#endif
    public class AutofacBusTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static AutofacBusTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("http://127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("http://127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void AutofacContainerTest()
        {
            string topicName = string.Format("test_autofac_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_autofac";

            var builder = new ContainerBuilder();
            builder.RegisterType<TestDependency>().As<ITestDependency>();
            var container = builder.Build();
            BusConfiguration busConfiguration = null;
            try
            {
                var objectBuilder = new AutofacObjectBuilder(container);

                busConfiguration =
                    new BusConfiguration(
                        objectBuilder,
                        new NewtonsoftJsonSerializer(typeof(JsonConverter).Assembly),
                        new MessageAuditorStub(),
                        new MessageTypeToTopicDictionary(new Dictionary<Type, string>
                                                         {
                                                             { typeof(TestMessage), topicName }
                                                         }),
                        new HandlerTypeToChannelDictionary(new Dictionary<Type, string>
                                                           {
                                                               { typeof(TestMessageHandler), channelName }
                                                           }),
                        defaultNsqLookupdHttpEndpoints: new[] { "127.0.0.1:4161" },
                        defaultThreadsPerHandler: 1,
                        nsqConfig: new Config
                        {
                            LookupdPollJitter = 0,
                            LookupdPollInterval = TimeSpan.FromSeconds(1)
                        },
                        preCreateTopicsAndChannels: true);

                busConfiguration.StartBus();

                var bus = objectBuilder.GetInstance<IBus>();

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

                Assert.IsInstanceOf<TestMessage>(messageInformation.DeserializedMessageBody);
                Assert.AreEqual(testMessage.Message, ((TestMessage)messageInformation.DeserializedMessageBody).Message);
                Assert.IsNull(testMessage.ModifiedMessage);

                Assert.AreEqual(topicName, messageInformation.Topic, "messageInformation.Topic");
                Assert.AreEqual(channelName, messageInformation.Channel, "messageInformation.Channel");
                Assert.AreNotEqual(Guid.Empty,
                                   messageInformation.UniqueIdentifier,
                                   "messageInformation.UniqueIdentifier");
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
            public string ModifiedMessage { get; set; }
        }

        private interface ITestDependency
        {
            void SetModifiedMessage(TestMessage message);
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class TestDependency : ITestDependency
        {
            public void SetModifiedMessage(TestMessage message)
            {
                if (message == null)
                    throw new ArgumentNullException("message");

                message.ModifiedMessage = string.Format("{0} - from TestDependency", message.Message);
            }
        }

        private class TestMessageHandler : IHandleMessages<TestMessage>
        {
            private static readonly List<IMessage> _messages = new List<IMessage>();
            private static readonly object _messagesLocker = new object();

            private static readonly List<ICurrentMessageInformation> _messagesInfos =
                new List<ICurrentMessageInformation>();

            private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

            private readonly IBus _bus;
            private readonly ITestDependency _testDependency;

            public TestMessageHandler(IBus bus, ITestDependency testDependency)
            {
                if (bus == null)
                    throw new ArgumentNullException("bus");
                if (testDependency == null)
                    throw new ArgumentNullException("testDependency");

                _bus = bus;
                _testDependency = testDependency;
            }

            public void Handle(TestMessage message)
            {
                _testDependency.SetModifiedMessage(message);
                Assert.AreEqual(message.Message + " - from TestDependency", message.ModifiedMessage);

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
