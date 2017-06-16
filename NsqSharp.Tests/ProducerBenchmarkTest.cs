using System;
using System.Diagnostics;
using NsqSharp.Api;
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
    public class ProducerBenchmarkTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static ProducerBenchmarkTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public void BenchmarkTcp1()
        {
            BenchmarkTcp(1);
        }

        [Test]
        public void BenchmarkTcp2()
        {
            BenchmarkTcp(2);
        }

        [Test]
        public void BenchmarkTcp4()
        {
            BenchmarkTcp(4);
        }

        [Test]
        public void BenchmarkTcp8()
        {
            BenchmarkTcp(8);
        }

        [Test]
        public void BenchmarkHttp1()
        {
            BenchmarkHttp(1);
        }

        [Test]
        public void BenchmarkHttp2()
        {
            BenchmarkHttp(2);
        }

        [Test]
        public void BenchmarkHttp4()
        {
            BenchmarkHttp(4);
        }

        [Test]
        public void BenchmarkHttp8()
        {
            BenchmarkHttp(8);
        }

        private void BenchmarkTcp(int parallel)
        {
            string topicName = "test_benchmark_" + DateTime.Now.UnixNano();

            try
            {
                const int benchmarkNum = 30000;

                byte[] body = new byte[512];

                var p = new Producer("127.0.0.1:4150");
                p.Connect();

                var startCh = new Chan<bool>();
                var wg = new WaitGroup();

                for (int j = 0; j < parallel; j++)
                {
                    wg.Add(1);
                    GoFunc.Run(() =>
                    {
                        startCh.Receive();
                        for (int i = 0; i < benchmarkNum / parallel; i++)
                        {
                            p.Publish(topicName, body);
                        }
                        wg.Done();
                    }, "ProducerBenchmarkTcpTest: sendLoop");
                }

                var stopwatch = Stopwatch.StartNew();
                startCh.Close();

                var done = new Chan<bool>();
                GoFunc.Run(() => { wg.Wait(); done.Send(true); }, "waiter and done sender");

                bool finished = false;
                Select
                    .CaseReceive(done, b => finished = b)
                    .CaseReceive(Time.After(TimeSpan.FromSeconds(10)), b => finished = false)
                    .NoDefault();

                stopwatch.Stop();

                if (!finished)
                {
                    Assert.Fail("timeout");
                }

                Console.WriteLine(string.Format("{0:#,0} sent in {1:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s; Threads: {3}",
                    benchmarkNum, stopwatch.Elapsed, benchmarkNum / stopwatch.Elapsed.TotalSeconds, parallel));

                p.Stop();
            }
            finally
            {
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private void BenchmarkHttp(int parallel)
        {
            string topicName = "test_benchmark_" + DateTime.Now.UnixNano();

            try
            {
                const int benchmarkNum = 30000;

                byte[] body = new byte[512];

                var startCh = new Chan<bool>();
                var wg = new WaitGroup();

                for (int j = 0; j < parallel; j++)
                {
                    wg.Add(1);
                    GoFunc.Run(() =>
                    {
                        startCh.Receive();
                        for (int i = 0; i < benchmarkNum / parallel; i++)
                        {
                            _nsqdHttpClient.Publish(topicName, body);
                        }
                        wg.Done();
                    }, "ProducerBenchmarkHttpTest: sendLoop");
                }

                var stopwatch = Stopwatch.StartNew();
                startCh.Close();
                wg.Wait();
                stopwatch.Stop();

                Console.WriteLine(string.Format("{0:#,0} sent in {1:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s; Threads: {3}",
                    benchmarkNum, stopwatch.Elapsed, benchmarkNum / stopwatch.Elapsed.TotalSeconds, parallel));
            }
            finally
            {
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }
    }
}
