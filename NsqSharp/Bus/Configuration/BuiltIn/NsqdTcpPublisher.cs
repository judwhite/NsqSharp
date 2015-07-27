using System;
using System.Collections.Generic;
using NsqSharp.Core;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>NSQD TCP publisher.</summary>
    public class NsqdTcpPublisher : INsqdPublisher
    {
        private readonly Producer _producer;

        /// <summary>
        ///     Initializes a new instance of the NsqSharp.Bus.Configuration.BuiltIn.NsqdTcpPublisher class.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="nsqdAddress">The nsqd address.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration.</param>
        public NsqdTcpPublisher(string nsqdAddress, ILogger logger, Config config)
        {
            if (string.IsNullOrEmpty(nsqdAddress))
                throw new ArgumentNullException("nsqdAddress");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (config == null)
                throw new ArgumentNullException("config");

            _producer = new Producer(nsqdAddress, logger, config);
        }

        /// <summary>Publishes a <paramref name="message"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        public void Publish(string topic, byte[] message)
        {
            _producer.Publish(topic, message);
        }

        /// <summary>Multi-Publishes <paramref name="messages"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        public void MultiPublish(string topic, IEnumerable<byte[]> messages)
        {
            _producer.MultiPublish(topic, messages);
        }

        /// <summary>Stops the nsqd publisher.</summary>
        public void Stop()
        {
            _producer.Stop();
        }
    }
}
