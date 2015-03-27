using System;
using System.Collections.Generic;
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

        private class TestMessage
        {
        }

        private class TestMessageHandler : IHandleMessages<TestMessage>
        {
            public void Handle(TestMessage message)
            {
            }
        }
    }
}
