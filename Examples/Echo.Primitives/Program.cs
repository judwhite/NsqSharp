using System;
using System.Text;
using NsqSharp;

namespace Echo.Primitives
{
    class Program
    {
        static void Main()
        {
            // Create a Producer. Producers must connect to nsqd directly (:4150)
            var producer = new Producer("127.0.0.1:4150");
            // Publish message "Hello!" to topic "test-topic-name"
            producer.Publish("test-topic-name", "Hello!");

            // Create a new Consumer listening to topic "test-topic-name" on channel "channel-name"
            var consumer = new Consumer("test-topic-name", "channel-name");
            // Add a handler to handle incoming messages
            consumer.AddHandler(new MessageHandler());
            // Consumers can connect to nsqd (:4150) or nsqlookupd (:4161)
            // When connecting to nsqlookupd to default polling interval for topic producers is 60s. This
            // can be modified with the constructor overload which takes a Config parameter.
            consumer.ConnectToNsqLookupd("127.0.0.1:4161");

            Console.WriteLine("Enter your message (blank line to quit):");

            // Get user input
            string line = Console.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                producer.Publish("test-topic-name", line);
                line = Console.ReadLine();
            }

            // Stop Producer/Consumer
            producer.Stop();
            consumer.Stop(blockUntilStopCompletes: true);
        }
    }

    public class MessageHandler : IHandler
    {
        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void HandleMessage(Message message)
        {
            string msg = Encoding.UTF8.GetString(message.Body);
            Console.WriteLine(string.Format("Echo: {0}", msg));
        }

        /// <summary>
        /// Called when a <see cref="Message"/> has exceeded the Consumer specified <see cref="Config.MaxAttempts"/>.
        /// </summary>
        /// <param name="message">The failed message.</param>
        public void LogFailedMessage(Message message)
        {
            string msg = Encoding.UTF8.GetString(message.Body);
            Console.WriteLine(string.Format("Message Failed: {0}", msg));
        }
    }
}
