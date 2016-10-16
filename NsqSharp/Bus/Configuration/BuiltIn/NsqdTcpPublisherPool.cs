using System;
using System.Collections.Generic;
using NsqSharp.Core;
using NsqSharp.Utils;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>NSQD TCP publisher pool.</summary>
    public class NsqdTcpPublisherPool : INsqdPublisher
    {
        private readonly ProducerPool _producerPool;

        public NsqdTcpPublisherPool(string nsqdAddress, ILogger logger, Config config)
        {
            if (string.IsNullOrEmpty(nsqdAddress))
                throw new ArgumentNullException("nsqdAddress");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (config == null)
                throw new ArgumentNullException("config");

            config = config.Clone();

            _producerPool = new ProducerPool(
                () =>
                {
                    var p = new Producer(nsqdAddress, logger, config);
                    p.Connect();
                    return p;
                }
            );
        }

        /// <summary>Publishes a <paramref name="message"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        public void Publish(string topic, byte[] message)
        {
            Producer p = null;
            try
            {
                p = _producerPool.Get();
                p.Publish(topic, message);
            }
            finally
            {
                _producerPool.Put(p);
            }
        }

        /// <summary>Multi-Publishes <paramref name="messages"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        public void MultiPublish(string topic, IEnumerable<byte[]> messages)
        {
            Producer p = null;
            try
            {
                p = _producerPool.Get();
                p.MultiPublish(topic, messages);
            }
            finally
            {
                _producerPool.Put(p);
            }
        }

        /// <summary>Stops the nsqd publisher.</summary>
        public void Stop()
        {
        ???
        }
    }
}
