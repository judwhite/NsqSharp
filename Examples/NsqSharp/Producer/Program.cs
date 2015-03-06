using System;
using System.Diagnostics;
using NsqSharp;

namespace ProducerExample
{
    class Program
    {
        static void Main()
        {
            var cfg = GetProducerExampleConfig();

            var producer = new Producer(cfg.NsqdTcpAddress);
            producer.Connect(); // optional; establishes connection before first publish

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < cfg.Count; i++)
            {
                producer.Publish(cfg.Topic, cfg.Message);
            }
            stopwatch.Stop();

            producer.Stop(); // graceful shutdown

            Console.WriteLine(string.Format("{0:#,0} message sent in {1:hh\\:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s",
                cfg.Count, stopwatch.Elapsed, cfg.Count / stopwatch.Elapsed.TotalSeconds));
        }

        private static ProducerExampleConfig GetProducerExampleConfig()
        {
            Console.Write("nsqd tcp address [127.0.0.1:4150]: ");
            string nsqdTcpAddress = Console.ReadLine();
            if (string.IsNullOrEmpty(nsqdTcpAddress))
                nsqdTcpAddress = "127.0.0.1:4150";

            Console.Write("topic [test]: ");
            string topic = Console.ReadLine();
            if (string.IsNullOrEmpty(topic))
                topic = "test";

            Console.Write("message [Hello world!]: ");
            string message = Console.ReadLine();
            if (string.IsNullOrEmpty(message))
                message = "Hello world!";

            int count;
            Console.Write("count [10000]: ");
            int.TryParse(Console.ReadLine().Replace(",", "").Replace(".", ""), out count);
            if (count <= 0)
                count = 10000;

            return new ProducerExampleConfig
                   {
                       NsqdTcpAddress = nsqdTcpAddress,
                       Topic = topic,
                       Message = message,
                       Count = count
                   };
        }

        public class ProducerExampleConfig
        {
            public string NsqdTcpAddress { get; set; }
            public string Topic { get; set; }
            public string Message { get; set; }
            public int Count { get; set; }
        }
    }
}
