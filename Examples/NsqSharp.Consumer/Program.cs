using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace NsqSharp.ConsumerExample
{
    internal class Program
    {
        private static readonly AutoResetEvent _wait = new AutoResetEvent(initialState: false);

        private static void Main()
        {
            // get consumer configuration from console
            var cfg = GetConsumerExampleConfig();

            // set outputstream
            Stream outputStream;
            if (cfg.FileName == "-")
                outputStream = Console.OpenStandardOutput();
            else
                outputStream = File.Open(cfg.FileName, FileMode.Append, FileAccess.Write);

            var streamWriter = new StreamWriter(outputStream);
            streamWriter.AutoFlush = true;

            Console.CancelKeyPress += Console_CancelKeyPress;

            // A Consumer subscribes to a topic and specifies the channel it wants to receive messages on. A topic is a stream
            // of messages created by a Producer. A channel is a copy of the topic's stream of messages for a specific client
            // (or cluster of clients). See http://nsq.io/overview/design.html for more information.

            // For each topic you want to listen to create a new Consumer.

            // All messages for a Consumer instance are passed to a single instance of a Handler. If you need to store state
            // related to the current message pass the message to a new instance of another class inside HandleMessage.

            // Call AddConcurrentHandlers to increase the number of concurrent handlers able to receive messages. This
            // is useful for long running non-I/O blocking handlers.

            // create a new consumer for the topic/channel and add a handler
            var consumer = new Consumer(cfg.Topic, cfg.Channel);
            consumer.AddHandler(new MessageHandler(streamWriter, cfg.MaxCount, _wait));

            // connect to nsqd or nsqlookupd. in practice you'd pick either nsqd or nsqlookupd depending on your configuration.

            // to test with nsqlookupd:
            // .\nsqlookupd
            // .\nsqd -lookupd-tcp-address=127.0.0.1:4160

            if (!string.IsNullOrEmpty(cfg.NsqdTcpAddress))
                consumer.ConnectToNSQD(cfg.NsqdTcpAddress);
            else
                consumer.ConnectToNSQLookupd(cfg.NsqlookupdHttpAddress);

            var stopwatch = Stopwatch.StartNew();
            _wait.WaitOne(); // wait for the # we want to receive or Ctrl+C
            stopwatch.Stop();

            consumer.Stop(blockUntilStopCompletes: true);

            streamWriter.Flush();

            var stats = consumer.Stats();

            Console.WriteLine(string.Format("{0:#,0} message received in {1:hh\\:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s",
                stats.MessagesReceived, stopwatch.Elapsed, stats.MessagesReceived / stopwatch.Elapsed.TotalSeconds));
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Cancelling...");
            e.Cancel = true;
            _wait.Set();
        }

        public class MessageHandler : IHandler
        {
            private readonly StreamWriter _writer;
            private readonly int _maxCount;
            private readonly AutoResetEvent _done;
            private int _count;

            public MessageHandler(StreamWriter writer, int maxCount, AutoResetEvent done)
            {
                _writer = writer;
                _maxCount = maxCount;
                _done = done;
            }

            public void HandleMessage(Message message)
            {
                if (Interlocked.Increment(ref _count) == _maxCount)
                    _done.Set();

                string msg = Encoding.UTF8.GetString(message.Body);
                _writer.WriteLine(string.Format("CNT:{0:#,0} TS:[{1:HH:mm:ss.fff}] MSG:[{2}]",
                    _count, message.Timestamp.ToLocalTime(), msg));
            }

            public void LogFailedMessage(Message message)
            {
                string msg = Encoding.UTF8.GetString(message.Body);
                Console.WriteLine(string.Format("[FAILED] TS:[{0:HH:mm:ss.fff}] MSG:[{1}]",
                    message.Timestamp.ToLocalTime(), msg));
            }
        }

        private static ConsumerExampleConfig GetConsumerExampleConfig()
        {
            string connectionType;
            do
            {
                Console.Write("nsqd [n] or nsqookupd [l]: ");
                connectionType = Console.ReadLine().ToLower();
            } while (connectionType != "n" && connectionType != "l");

            string nsqdTcpAddress = null;
            string nsqlookupdHttpAddress = null;
            if (connectionType == "n")
            {
                Console.Write("nsqd tcp address [127.0.0.1:4150]: ");
                nsqdTcpAddress = Console.ReadLine();
                if (string.IsNullOrEmpty(nsqdTcpAddress))
                    nsqdTcpAddress = "127.0.0.1:4150";
            }
            else
            {
                Console.Write("nsqlookupd http address [127.0.0.1:4161]: ");
                nsqlookupdHttpAddress = Console.ReadLine();
                if (string.IsNullOrEmpty(nsqlookupdHttpAddress))
                    nsqlookupdHttpAddress = "127.0.0.1:4161";
            }

            Console.Write("topic [test]: ");
            string topic = Console.ReadLine();
            if (string.IsNullOrEmpty(topic))
                topic = "test";

            Console.Write("channel [test-channel]: ");
            string channel = Console.ReadLine();
            if (string.IsNullOrEmpty(channel))
                channel = "test-channel";

            int maxCount;
            Console.Write("count before issuing stop [10000]: ");
            int.TryParse(Console.ReadLine().Replace(",", "").Replace(".", ""), out maxCount);
            if (maxCount <= 0)
                maxCount = 10000;

            Console.Write("output filename ['-' for stdout]: ");
            string fileName = Console.ReadLine();
            if (string.IsNullOrEmpty(fileName))
                fileName = "-";

            return new ConsumerExampleConfig
                   {
                       NsqdTcpAddress = nsqdTcpAddress,
                       NsqlookupdHttpAddress = nsqlookupdHttpAddress,
                       Topic = topic,
                       Channel = channel,
                       MaxCount = maxCount,
                       FileName = fileName
                   };
        }

        public class ConsumerExampleConfig
        {
            public string NsqdTcpAddress { get; set; }
            public string NsqlookupdHttpAddress { get; set; }
            public string Topic { get; set; }
            public string Channel { get; set; }
            public int MaxCount { get; set; }
            public string FileName { get; set; }
        }
    }
}
