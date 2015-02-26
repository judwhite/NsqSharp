using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace NsqSharp.Producer.Http
{
    class Program
    {
        static void Main()
        {
            var cfg = GetProducerExampleConfig();

            string address = string.Format("http://{0}/pub?topic={1}", cfg.NsqdHttpAddress, cfg.Topic);
            byte[] message = Encoding.UTF8.GetBytes(cfg.Message);

            var webClient = new WebClient();
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < cfg.Count; i++)
            {
                webClient.UploadData(address, message);
            }
            stopwatch.Stop();

            Console.WriteLine(string.Format("{0:#,0} message sent in {1}; Avg: {2:#,0} msgs/s",
                cfg.Count, stopwatch.Elapsed, cfg.Count / stopwatch.Elapsed.TotalSeconds));
        }

        private static ProducerExampleConfig GetProducerExampleConfig()
        {
            Console.Write("nsqd http address [127.0.0.1:4151]: ");
            string nsqdHttpAddress = Console.ReadLine();
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                nsqdHttpAddress = "127.0.0.1:4151";

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
                NsqdHttpAddress = nsqdHttpAddress,
                Topic = topic,
                Message = message,
                Count = count
            };
        }

        public class ProducerExampleConfig
        {
            public string NsqdHttpAddress { get; set; }
            public string Topic { get; set; }
            public string Message { get; set; }
            public int Count { get; set; }
        }
    }
}
