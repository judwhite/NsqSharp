﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using NsqSharp.Api;
using NsqSharp.Tests.TestHelpers;
using NsqSharp.Tests.Utils.Extensions;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests
{
#if !RUN_INTEGRATION_TESTS
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
#else
    [TestFixture]
#endif
    public class ConsumerRdyRedistributionTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient1;
        private static readonly NsqdHttpClient _nsqdHttpClient2;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static ConsumerRdyRedistributionTest()
        {
            _nsqdHttpClient1 = new NsqdHttpClient("127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqdHttpClient2 = new NsqdHttpClient("127.0.0.1:5151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        [Ignore("Long Running Test")]
        public void TestRdyRedistribution()
        {
            var results = TestRdyRedistribution(
                rdyRedistributeOnIdle: true,
                maxInFlight: 4,
                rdyRedistributeInterval: TimeSpan.FromSeconds(5),
                lowRdyIdleTimeout: TimeSpan.FromMinutes(5),
                handlerSleepTime: TimeSpan.FromSeconds(10),
                sleepBeforeIdlePublish: TimeSpan.FromMinutes(12),
                testDuration: TimeSpan.FromMinutes(30),
                startWithInitialMessageOnIdleNsqd: true,
                numberOfMessagesToSendOnIdleNsqd: 2,
                numberOfMessages: 10000
            );

            // expected max without redistribute: 360+3
            // ideal: 720 - ((5*12)*2) + 3 = 603
            // actual: 600

            foreach (var item in results)
            {
                Console.WriteLine("[{0}] {1} {2}", item.HandlerStartTime.Formatted(), item.NsqdAddress, item.Message);
            }

            var p1 = results.Where(p => p.NsqdAddress.Contains(":4150")).ToList();
            var p2 = results.Where(p => p.NsqdAddress.Contains(":5150")).ToList();

            Assert.AreEqual(3, p1.Count, "p1.Count");
            Assert.GreaterOrEqual(580, p2.Count, "p2.Count");
        }

        [Test]
        public void TestRdyRedistributionMaxInFlight1()
        {
            var results = TestRdyRedistribution(
                rdyRedistributeOnIdle: true,
                maxInFlight: 1,
                rdyRedistributeInterval: TimeSpan.FromSeconds(5),
                lowRdyIdleTimeout: TimeSpan.FromSeconds(10),
                handlerSleepTime: TimeSpan.FromMilliseconds(250),
                sleepBeforeIdlePublish: TimeSpan.FromSeconds(20),
                testDuration: TimeSpan.FromSeconds(60),
                startWithInitialMessageOnIdleNsqd: true,
                numberOfMessagesToSendOnIdleNsqd: 2,
                numberOfMessages: 30
            );

            var p1 = results.Where(p => p.NsqdAddress.Contains(":4150")).ToList();
            var p2 = results.Where(p => p.NsqdAddress.Contains(":5150")).ToList();

            Assert.AreEqual(3, p1.Count, "p1.Count");
            Assert.AreEqual(30, p2.Count, "p2.Count");
        }

        private static List<TestResults> TestRdyRedistribution(
            bool rdyRedistributeOnIdle,
            int maxInFlight,
            TimeSpan rdyRedistributeInterval,
            TimeSpan lowRdyIdleTimeout,
            TimeSpan handlerSleepTime,
            TimeSpan sleepBeforeIdlePublish,
            TimeSpan testDuration,
            bool startWithInitialMessageOnIdleNsqd,
            int numberOfMessagesToSendOnIdleNsqd,
            int numberOfMessages
        )
        {
            string topicName = string.Format("test_rdy_redistribution_{0}", DateTime.Now.UnixNano());

            try
            {
                _nsqdHttpClient1.CreateTopic(topicName);
                _nsqdHttpClient2.CreateTopic(topicName);
                _nsqLookupdHttpClient.CreateTopic(topicName);

                Producer p1 = new Producer("127.0.0.1:4150");
                if (startWithInitialMessageOnIdleNsqd)
                {
                    Console.WriteLine("[{0}] Sending initial message on 4150...", DateTime.Now.Formatted());
                    p1.Publish(topicName, "initial");
                }

                Console.WriteLine("[{0}] Sending messages on 5150...", DateTime.Now.Formatted());

                Producer p2 = new Producer("127.0.0.1:5150");
                for (int i = 0; i < numberOfMessages; i++)
                {
                    p2.Publish(topicName, i.ToString());
                }

                Consumer c = new Consumer(
                    topicName,
                    "test-channel",
                    new TestConsoleLogger(),
                    new Config
                    {
                        MaxInFlight = maxInFlight,
                        LowRdyIdleTimeout = lowRdyIdleTimeout,
                        RDYRedistributeInterval = rdyRedistributeInterval,
                        RDYRedistributeOnIdle = rdyRedistributeOnIdle
                    }
                );
                var messageHandler = new MessageHandler(handlerSleepTime);
                c.AddHandler(messageHandler, threads: maxInFlight);
                c.ConnectToNsqLookupd("127.0.0.1:4161");

                Thread.Sleep(sleepBeforeIdlePublish);

                Console.WriteLine("[{0}] Sending messages on 4150...", DateTime.Now.Formatted());

                for (int i = 1; i <= numberOfMessagesToSendOnIdleNsqd; i++)
                {
                    p1.Publish(topicName, string.Format("{0} - snuck in!", i));
                }

                Thread.Sleep(testDuration - sleepBeforeIdlePublish);

                Console.WriteLine("[{0}] Stopping...", DateTime.Now.Formatted());

                p1.Stop();
                p2.Stop();

                c.Stop();

                return messageHandler.GetTestResults();
            }
            finally
            {
                _nsqdHttpClient1.DeleteTopic(topicName);
                _nsqdHttpClient2.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        public class MessageHandler : IHandler
        {
            private readonly TimeSpan _sleepTime;
            private readonly List<TestResults> _testResults;
            private readonly object _testResultsLocker = new object();

            public MessageHandler(TimeSpan sleepTime)
            {
                _sleepTime = sleepTime;
                _testResults = new List<TestResults>();
            }

            public void HandleMessage(IMessage message)
            {
                var startTime = DateTime.Now;
                string body = Encoding.UTF8.GetString(message.Body);

                Console.WriteLine("[{0}] {1} {2}", startTime.Formatted(), message.NsqdAddress, body);

                lock (_testResultsLocker)
                {
                    _testResults.Add(new TestResults
                    {
                        HandlerStartTime = startTime,
                        NsqdAddress = message.NsqdAddress,
                        Message = body
                    });
                }

                Thread.Sleep(_sleepTime);
            }

            public void LogFailedMessage(IMessage message)
            {
            }

            public List<TestResults> GetTestResults()
            {
                return _testResults;
            }
        }

        [DebuggerDisplay("{HandlerStartTime} {NsqdAddress} {Message}")]
        public class TestResults
        {
            public DateTime HandlerStartTime { get; set; }
            public string NsqdAddress { get; set; }
            public string Message { get; set; }
        }
    }
}
