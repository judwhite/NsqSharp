using System;
using System.Diagnostics;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NUnit.Framework;

namespace NsqSharp.Tests
{
    [TestFixture(IgnoreReason = "Live Benchmark")]
    public class ProducerBenchmarkTest
    {
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

        private void BenchmarkTcp(int parallel)
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
                //int localj = j;
                GoFunc.Run(() =>
                {
                    startCh.Receive();
                    for (int i = 0; i < benchmarkNum / parallel; i++)
                    {
                        //if (i%10 == 0)
                        //{
                        //    Debug.WriteLine(string.Format("{0}: {1}/{2}", localj, i, benchmarkNum/parallel));
                        //}
                        p.Publish("test", body);
                    }
                    wg.Done();
                }, "ProducerBenchmarkTcpTest: sendLoop");
            }

            var stopwatch = Stopwatch.StartNew();
            startCh.Close();
            wg.Wait();
            stopwatch.Stop();

            Console.WriteLine(string.Format("{0:#,0} sent in {1:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s; Threads: {3}",
                benchmarkNum, stopwatch.Elapsed, benchmarkNum / stopwatch.Elapsed.TotalSeconds, parallel));
        }

        private void BenchmarkHttp(int parallel)
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
                        NsqdHttpApi.Publish("127.0.0.1:4151", "test", body);
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
    }
}
