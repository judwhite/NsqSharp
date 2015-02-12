using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NsqSharp.Channels;
using NsqSharp.Extensions;
using NsqSharp.Utils;
using NUnit.Framework;

namespace NsqSharp.Tests
{
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
    public class ProducerTest
    {
        [Test]
        public void TestProducerConnection()
        {
            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
            w.SetLogger(new ConsoleLogger(), LogLevel.Debug);

            w.Publish("write_test", "test");

            w.Stop();

            Assert.Throws<ErrStopped>(() => w.Publish("write test", "fail test"));
        }

        [Test]
        public void TestProducerPublish()
        {
            var topicName = "publish" + DateTime.Now.Unix();
            const int msgCount = 10;

            var config = new Config();
            var w = new Producer("127.0.0.1:4150", config);
            w.SetLogger(new ConsoleLogger(), LogLevel.Debug);
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
            }
        }

        [Test]
        public void TestProducerMultiPublish()
        {
            var topicName = "multi_publish" + DateTime.Now.Unix();
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

                w.MultiPublish(topicName, testData);
                w.Publish(topicName, "bad_test_case");

                readMessages(topicName, msgCount);
            }
            finally
            {
                w.Stop();
            }
        }

        [Test]
        public void TestProducerPublishAsync()
        {
            var topicName = "async_publish" + DateTime.Now.Unix();
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
            }
        }

        [Test]
        public void TestProducerMultiPublishAsync()
        {
            var topicName = "multi_publish" + DateTime.Now.Unix();
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
            }
        }

        [Test]
        public void TestProducerHeartbeat()
        {
            var topicName = "heartbeat" + DateTime.Now.Unix();

            var config = new Config();
            config.HeartbeatInterval = TimeSpan.FromMilliseconds(100);
            var w = new Producer("127.0.0.1:4150", config);
            w.SetLogger(new ConsoleLogger(), LogLevel.Debug);

            try
            {
                ErrIdentify errIdentify = null;
                try
                {
                    w.Publish(topicName, "publish_test_case");
                }
                catch (ErrIdentify ex)
                {
                    errIdentify = ex;
                }

                Assert.IsNotNull(errIdentify, "ErrIdentify not thrown");
                Assert.AreEqual("E_BAD_BODY IDENTIFY heartbeat interval (100) is invalid", errIdentify.Reason);
            }
            finally
            {
                w.Stop();
            }

            try
            {
                config = new Config();
                config.HeartbeatInterval = TimeSpan.FromMilliseconds(1000);
                w = new Producer("127.0.0.1:4150", config);
                w.SetLogger(new ConsoleLogger(), LogLevel.Debug);

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
            }
        }

        private void readMessages(string topicName, int msgCount)
        {
            var config = new Config();
            config.DefaultRequeueDelay = TimeSpan.Zero;
            config.MaxBackoffDuration = TimeSpan.FromMilliseconds(50);
            var q = new Consumer(topicName, "ch", config);
            q.SetLogger(new ConsoleLogger(), LogLevel.Debug);

            var h = new ConsumerHandler { q = q };
            q.AddHandler(h);

            q.ConnectToNSQD("127.0.0.1:4150");
            q.StopChan.Receive();

            Assert.AreEqual(msgCount, h.messagesGood, "should have handled a diff number of messages");
            Assert.AreEqual(1, h.messagesFailed, "failed message not done");
        }
    }

    internal class ConsumerHandler : IHandler, IFailedMessageLogger
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
