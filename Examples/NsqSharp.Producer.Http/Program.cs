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

            Console.WriteLine(string.Format("{0:#,0} message sent in {1:hh\\:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s",
                cfg.Count, stopwatch.Elapsed, cfg.Count / stopwatch.Elapsed.TotalSeconds));
        }

        private static ProducerExampleConfig GetProducerExampleConfig()
        {
            Console.Write("nsqd http address [127.0.0.1:4151]: ");
            string nsqdHttpAddress = Console.ReadLine();
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                nsqdHttpAddress = "127.0.0.1:4151";

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
