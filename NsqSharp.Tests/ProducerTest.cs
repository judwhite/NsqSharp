using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NsqSharp.Core;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Extensions;
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
            string topicName = "write_test" + DateTime.Now.Unix();

            try
            {
                var config = new Config();
                var w = new Producer("127.0.0.1:4150", config);

                w.Publish(topicName, "test");

                w.Stop();

                Assert.Throws<ErrStopped>(() => w.Publish(topicName, "fail test"));
            }
            finally
            {
                NsqdHttpApi.DeleteTopic("127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void TestProducerPublish()
        {
            var topicName = "publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
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
                NsqdHttpApi.DeleteTopic("127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void TestProducerMultiPublish()
        {
            var topicName = "multi_publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
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
                NsqdHttpApi.DeleteTopic("127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void TestProducerPublishAsync()
        {
            var topicName = "async_publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
            try
            {
                var tasks = new List<Task<ProducerResponse>>();
                for (int i = 0; i < msgCount; i++)
                {
                    var task = w.PublishAsync(topicName, "publish_test_case", "test", i);
                    tasks.Add(task);
                }

                for (int i = 0; i < msgCount; i++)
                {
                    tasks[i].Wait();
                    var trans = tasks[i].Result;

                    Assert.IsNull(trans.Error);
                    Assert.IsNotNull(trans.Args);
                    Assert.AreEqual(2, trans.Args.Length);
                    Assert.AreEqual("test", trans.Args[0]);
                    Assert.AreEqual(i, trans.Args[1]);
                }

                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount);
            }
            finally
            {
                w.Stop();
                NsqdHttpApi.DeleteTopic("127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void TestProducerMultiPublishAsync()
        {
            var topicName = "multi_publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
            try
            {
                var testData = new List<byte[]>();
                for (int i = 0; i < msgCount; i++)
                {
                    testData.Add(Encoding.UTF8.GetBytes("multipublish_test_case"));
                }

                var responseTask = w.MultiPublishAsync(topicName, testData, "test0", 1);

                responseTask.Wait();
                var trans = responseTask.Result;

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
                NsqdHttpApi.DeleteTopic("127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        [Test]
        public void TestProducerHeartbeat()
        {
            var topicName = "heartbeat" + DateTime.Now.Unix();

            var config = new Config();
            config.HeartbeatInterval = TimeSpan.FromMilliseconds(100);
            var w = new Producer("127.0.0.1:4150", config);

            try
            {
                ErrIdentify errIdentify = Assert.Throws<ErrIdentify>(() => w.Publish(topicName, "publish_test_case"));

                Assert.AreEqual("E_BAD_BODY IDENTIFY heartbeat interval (100) is invalid", errIdentify.Reason);
            }
            finally
            {
                w.Stop();
                // note: if test successful, topic will not be created - don't need to delete
            }

            try
            {
                config = new Config();
                config.HeartbeatInterval = TimeSpan.FromMilliseconds(1000);
                w = new Producer("127.0.0.1:4150", config);

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
                NsqdHttpApi.DeleteTopic("127.0.0.1:4151", topicName);
                NsqdHttpApi.DeleteTopic("127.0.0.1:4161", topicName);
            }
        }

        private void readMessages(string topicName, int msgCount)
        {
            var config = new Config();
            config.DefaultRequeueDelay = TimeSpan.Zero;
            config.MaxBackoffDuration = TimeSpan.FromMilliseconds(50);
            var q = new Consumer(topicName, "ch", config);

            var h = new ConsumerHandler { q = q };
            q.AddHandler(h);

            q.ConnectToNsqd("127.0.0.1:4150");
            q.Wait();

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

        public void LogFailedMessage(IMessage message)
        {
            messagesFailed++;
            q.StopAsync();
        }

        public void HandleMessage(IMessage message)
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
