using System;
using System.Diagnostics;

namespace NsqSharp.ProducerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var cfg = GetProducerExampleConfig();

            var producer = new Producer(cfg.NsqdAddress);
            producer.Connect(); // optional; establishes connection before first publish

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < cfg.Count; i++)
            {
                producer.Publish(cfg.Topic, cfg.Message);
            }
            stopwatch.Stop();

            Console.WriteLine(string.Format("{0:#,0} message sent in {1}; Avg: {2:#,0} msgs/s",
                cfg.Count, stopwatch.Elapsed, cfg.Count / stopwatch.Elapsed.TotalSeconds));
        }

        private static ProducerExampleConfig GetProducerExampleConfig()
        {
            Console.Write("nsqd address [127.0.0.1:4150]: ");
            string nsqdAddress = Console.ReadLine();
            if (string.IsNullOrEmpty(nsqdAddress))
                nsqdAddress = "127.0.0.1:4150";

            string topic;
            do
            {
                Console.Write("topic: ");
                topic = Console.ReadLine();
            } while (string.IsNullOrEmpty(topic));

            string message;
            do
            {
                Console.Write("message: ");
                message = Console.ReadLine();
            } while (string.IsNullOrEmpty(message));

            int count;
            do
            {
                Console.Write("count: ");
                int.TryParse(Console.ReadLine(), out count);
            } while (count <= 0);

            return new ProducerExampleConfig
            {
                NsqdAddress = nsqdAddress,
                Topic = topic,
                Message = message,
                Count = count
            };
        }

        public class ProducerExampleConfig
        {
            public string NsqdAddress { get; set; }
            public string Topic { get; set; }
            public string Message { get; set; }
            public int Count { get; set; }
        }
    }
}
