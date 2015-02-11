using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using NsqSharp.Channels;
using NsqSharp.Extensions;
using NsqSharp.Tests.Utils;
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
            w.SetLogger(new NullLogger(), LogLevel.Info);

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
            w.SetLogger(new NullLogger(), LogLevel.Info);
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
            w.SetLogger(new NullLogger(), LogLevel.Info);
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
            w.SetLogger(new NullLogger(), LogLevel.Info);
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

        public void TestProducerMultiPublishAsync()
        {
            // TODO
        }

        public void TestProducerHeartbeat()
        {
            // TODO
        }

        private void readMessages(string topicName, int msgCount)
        {
            // TODO: some of these tests are useless without implementing this method; depends on Consumer
        }
    }

    internal class ConsumerHandler
    {
        //public Consumer q { get; set; } // TODO
        public int messagesGood { get; set; }
        public int messagesFailed { get; set; }

        public void LogFailedMessage(Message message)
        {
            // TODO
            messagesFailed++;
            //q.Stop();
        }

        public void HandleMessage(Message message)
        {
            var msg = Encoding.UTF8.GetString(message.Body);
            if (msg == "bad_test_case")
            {
                throw new FailThisMessageException();
            }
            if (msg != "multipublish_test_case" && msg != "public_test_case")
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
