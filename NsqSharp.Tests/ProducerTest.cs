using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Extensions;
using NsqSharp.Utils.Loggers;
using NUnit.Framework;

namespace NsqSharp.Tests
{
#if !RUN_INTEGRATION_TESTS
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
#else
    [TestFixture]
#endif
    public class ProducerTest
    {
        [Test]
        public void TestProducerConnection()
        {
            try
            {
                var config = new Config();
                var w = new Producer("127.0.0.1:4150", new ConsoleLogger(LogLevel.Debug), config);

                w.Publish("write_test", "test");

                w.Stop();

                Assert.Throws<ErrStopped>(() => w.Publish("write test", "fail test"));
            }
            finally
            {
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", "write_test");
            }
        }

        // TODO: Is Ping really useful?
        /*[Test]
        public void TestProducerPing()
        {
            var config = new Config();
            var w = new Producer("127.0.0.1:4150", new ConsoleLogger(LogLevel.Debug), config);

            w.Ping();

            w.Stop();

            Assert.Throws<ErrStopped>(w.Ping, "should not be able to ping after Stop()");
        }*/

        [Test]
        public void TestProducerPublish()
        {
            var topicName = "publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", new ConsoleLogger(LogLevel.Debug), config);
            try
            {
                for (int i = 0; i < msgCount; i++)
                {
                    w.Publish(topicName, "publish_test_case");
                }

                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount);
            }
            finally
            {
                w.Stop();
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void TestProducerMultiPublish()
        {
            var topicName = "multi_publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", new ConsoleLogger(LogLevel.Debug), config);
            try
            {
                var testData = new List<byte[]>();
                for (int i = 0; i < msgCount; i++)
                {
                    testData.Add(Encoding.UTF8.GetBytes("multipublish_test_case"));
                }

                w.MultiPublish(topicName, testData);
                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount);
            }
            finally
            {
                w.Stop();
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test, Ignore("PublishAsync made private")]
        public void TestProducerPublishAsync()
        {
            /*var topicName = "async_publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
            w.SetLogger(new ConsoleLogger(), LogLevel.Debug);
            try
            {
                var responseChan = new Chan<ProducerTransaction>(msgCount);

                for (int i = 0; i < msgCount; i++)
                {
                    w.PublishAsync(topicName, "publish_test_case", responseChan, "test");
                }

                for (int i = 0; i < msgCount; i++)
                {
                    var trans = responseChan.Receive();

                    Assert.IsNull(trans.Error);
                    Assert.IsNotNull(trans.Args);
                    Assert.AreEqual(1, trans.Args.Length);
                    Assert.AreEqual("test", trans.Args[0]);
                }

                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount);
            }
            finally
            {
                w.Stop();
            }*/
        }

        [Test, Ignore("MultiPublishAsync made private")]
        public void TestProducerMultiPublishAsync()
        {
            /*var topicName = "multi_publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
            w.SetLogger(new ConsoleLogger(), LogLevel.Debug);
            try
            {
                var testData = new List<byte[]>();
                for (int i = 0; i < msgCount; i++)
                {
                    testData.Add(Encoding.UTF8.GetBytes("multipublish_test_case"));
                }

                var responseChan = new Chan<ProducerTransaction>(msgCount);
                w.MultiPublishAsync(topicName, testData, responseChan, "test0", 1);

                var trans = responseChan.Receive();

                Assert.IsNull(trans.Error);
                Assert.IsNotNull(trans.Args);
                Assert.AreEqual(2, trans.Args.Length);
                Assert.AreEqual("test0", trans.Args[0]);
                Assert.AreEqual(1, trans.Args[1]);

                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount);
            }
            finally
            {
                w.Stop();
            }*/
        }

        [Test]
        public void TestProducerHeartbeat()
        {
            var topicName = "heartbeat" + DateTime.Now.Unix();

            var config = new Config();
            config.HeartbeatInterval = TimeSpan.FromMilliseconds(100);
            var w = new Producer("127.0.0.1:4150", new ConsoleLogger(LogLevel.Debug), config);

            try
            {
                ErrIdentify errIdentify = Assert.Throws<ErrIdentify>(() => w.Publish(topicName, "publish_test_case"));

                Assert.AreEqual("E_BAD_BODY IDENTIFY heartbeat interval (100) is invalid", errIdentify.Reason);
            }
            finally
            {
                w.Stop();
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }

            try
            {
                config = new Config();
                config.HeartbeatInterval = TimeSpan.FromMilliseconds(1000);
                w = new Producer("127.0.0.1:4150", new ConsoleLogger(LogLevel.Debug), config);

                w.Publish(topicName, "publish_test_case");

                // TODO: what are we testing here?
                Thread.Sleep(1100);

                const int msgCount = 10;
                for (int i = 0; i < msgCount; i++)
                {
                    w.Publish(topicName, "publish_test_case");
                }

                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount + 1);
            }
            finally
            {
                w.Stop();
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        private void readMessages(string topicName, int msgCount)
        {
            var config = new Config();
            config.DefaultRequeueDelay = TimeSpan.Zero;
            config.MaxBackoffDuration = TimeSpan.FromMilliseconds(50);
            var q = new Consumer(topicName, "ch", new ConsoleLogger(LogLevel.Debug), config);

            var h = new ConsumerHandler { q = q };
            q.AddHandler(h);

            q.ConnectToNsqd("127.0.0.1:4150");
            q.StopChan.Receive();

            Assert.AreEqual(msgCount, h.messagesGood, "should have handled a diff number of messages");
            Assert.AreEqual(1, h.messagesFailed, "failed message not done");
        }
    }

    public class MockProducerConn
    {
        private static readonly byte[] PUB_BYTES = Encoding.UTF8.GetBytes("PUB");
        private static readonly byte[] OK_RESP = mockNSQD.framedResponse((int)FrameType.Response, Encoding.UTF8.GetBytes("OK"));

        private readonly IConnDelegate _connDelegate;
        private readonly Chan<bool> _closeCh;
        private readonly Chan<bool> _pubCh;

        public MockProducerConn(IConnDelegate connDelegate)
        {
            _connDelegate = connDelegate;
            _closeCh = new Chan<bool>();
            _pubCh = new Chan<bool>();
            GoFunc.Run(router, "ProducerTest:router");
        }

        public override string ToString()
        {
            return "127.0.0.1:0";
        }

        public void SetLogger(ILogger l, string format)
        {
        }

        public IdentifyResponse Connect()
        {
            return new IdentifyResponse();
        }

        public void Close()
        {
            _closeCh.Close();
        }

        public void WriteCommand(Command command)
        {
            if (command.Name.SequenceEqual(PUB_BYTES))
            {
                _pubCh.Send(true);
            }
        }

        private void router()
        {
            bool doLoop = true;
            using (var select =
                Select
                    .CaseReceive(_closeCh, o => doLoop = false)
                    .CaseReceive(_pubCh, o => _connDelegate.OnResponse(null, OK_RESP))
                    .NoDefault(defer: true))
            {
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                while (doLoop)
                {
                    select.Execute();
                }
            }
        }
    }

    internal class ConsumerHandler : IHandler
    {
        public Consumer q { get; set; }
        public int messagesGood { get; set; }
        public int messagesFailed { get; set; }

        public void LogFailedMessage(Message message)
        {
            messagesFailed++;
            q.Stop();
        }

        public void HandleMessage(Message message)
        {
            var msg = Encoding.UTF8.GetString(message.Body);
            if (msg == "bad_test_case")
            {
                throw new FailThisMessageException();
            }
            if (msg != "multipublish_test_case" && msg != "publish_test_case")
            {
                throw new Exception(string.Format("message 'action' was not correct {0}", msg));
            }
            messagesGood++;
        }
    }

    [Serializable]
    internal class FailThisMessageException : Exception
    {
        public FailThisMessageException()
        {
        }

        protected FailThisMessageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    internal class IncorrectMessageException : Exception
    {
        public IncorrectMessageException()
        {
        }

        protected IncorrectMessageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
