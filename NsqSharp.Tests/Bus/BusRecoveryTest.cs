using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Api;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Core;
using NsqSharp.Tests.Bus.TestFakes;
using NsqSharp.Tests.TestHelpers;
using NsqSharp.Utils;
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
    public class BusRecoveryTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        private static readonly WaitGroup _wg = new WaitGroup();
        //private static bool _handlerFinished;

        static BusRecoveryTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("http://127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("http://127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void TestRecoveryAfterErrProtocol()
        {
            string topicName = string.Format("test_busrecovery_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_busrecovery";

            var container = new Container();

            _nsqdHttpClient.CreateTopic(topicName);
            _nsqLookupdHttpClient.CreateTopic(topicName);

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
                    nsqLogger: new TestConsoleLogger(),
                    preCreateTopicsAndChannels: true,
                    nsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(300)
                    }
                ));

                var bus = container.GetInstance<IBus>();

                Assert.Throws<ErrProtocol>(() => bus.Send(new TestMessage { Bytes = new byte[1024 * 1024 + 1] }));

                bool successfulBusSend = false;
                int i;
                var start = DateTime.Now;
                _wg.Add(1);
                for (i = 0; i < 50; i++)
                {
                    try
                    {
                        bus.Send(new TestMessage
                        {
                            Bytes = Encoding.UTF8.GetBytes(string.Format("recovered {0}", i + 1))
                        });
                        successfulBusSend = true;
                        break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    }
                }

                Assert.IsTrue(successfulBusSend, "successfulBusSend");

                Console.WriteLine("*** Recovered in {0} ***", DateTime.Now - start);

                _wg.Wait();

                var message = TestMessageHandler.LastMessage;
                Assert.IsNotNull(message, "TestMessageHandler.LastMessage");
                Assert.IsNotNull(message, "TestMessageHandler.LastMessage.Bytes");
                Assert.AreEqual(Encoding.UTF8.GetString(message.Bytes), string.Format("recovered {0}", i + 1));
            }
            finally
            {
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private class TestMessage
        {
            public byte[] Bytes { get; set; }
        }

        private class TestMessageHandler : IHandleMessages<TestMessage>
        {
            public static TestMessage LastMessage { get; set; }

            public void Handle(TestMessage message)
            {
                LastMessage = message;
                _wg.Done();
            }
        }
    }
}
