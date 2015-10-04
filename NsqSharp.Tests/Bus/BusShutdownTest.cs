using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using NsqSharp.Api;
using NsqSharp.Bus;
using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.BuiltIn;
using NsqSharp.Tests.Bus.TestFakes;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
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
    public class BusShutdownTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        private static readonly WaitGroup _wg = new WaitGroup();
        private static bool _handlerFinished;

        static BusShutdownTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("http://127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("http://127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void TestBusShutdown()
        {
            string topicName = string.Format("test_busshutdown_{0}", DateTime.Now.UnixNano());
            const string channelName = "test_busshutdown";

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
                    nsqConfig: new Config
                    {
                        LookupdPollJitter = 0,
                        LookupdPollInterval = TimeSpan.FromSeconds(1)
                    }
                ));

                var bus = container.GetInstance<IBus>();

                _wg.Add(1);
                bus.Send(new TestMessage());
                _wg.Wait();

                var stoppedChan = new Chan<bool>(1);
                GoFunc.Run(() =>
                           {
                               var start = DateTime.Now;
                               BusService.Stop();
                               Console.WriteLine(string.Format("Shutdown occurred in {0}", DateTime.Now - start));
                               stoppedChan.Send(true);
                           }, "bus stopper and stopped notifier");

                bool timeout = false;
                Select
                    .CaseReceive(stoppedChan, o => { })
                    .CaseReceive(Time.After(TimeSpan.FromMilliseconds(90000)), o => { timeout = true; })
                    .NoDefault();

                Assert.IsFalse(timeout, "timeout");
                Assert.IsFalse(_handlerFinished, "handlerFinished");
            }
            finally
            {
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private class TestMessage
        {
        }

        private class TestMessageHandler : IHandleMessages<TestMessage>
        {
            public void Handle(TestMessage message)
            {
                _wg.Done();
                Thread.Sleep(-1);
                _handlerFinished = true;
            }
        }
    }
}
