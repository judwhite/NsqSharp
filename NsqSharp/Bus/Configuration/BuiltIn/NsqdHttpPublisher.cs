using System;
using System.Collections.Generic;
using NsqSharp.Api;

namespace NsqSharp.Bus.Configuration.BuiltIn
{
    /// <summary>NSQD HTTP publisher.</summary>
    public class NsqdHttpPublisher : INsqdPublisher
    {
        private readonly NsqdHttpClient _nsqdHttpClient;

        /// <summary>Initializes a new instance of the <see cref="NsqdHttpPublisher"/> class.</summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nsqdHttpEndpoint"/> is null or empty.
        /// </exception>
        /// <param name="nsqdHttpEndpoint">The nsqd HTTP endpoint.</param>
        /// <param name="httpRequestTimeout">The HTTP request timeout.</param>
        public NsqdHttpPublisher(string nsqdHttpEndpoint, TimeSpan httpRequestTimeout)
        {
            if (string.IsNullOrEmpty(nsqdHttpEndpoint))
                throw new ArgumentNullException("nsqdHttpEndpoint");

            _nsqdHttpClient = new NsqdHttpClient(nsqdHttpEndpoint, httpRequestTimeout);
        }

        /// <summary>Publishes a <paramref name="message"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        public void Publish(string topic, byte[] message)
        {
            _nsqdHttpClient.Publish(topic, message);
        }

        /// <summary>Multi-Publishes <paramref name="messages"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        public void MultiPublish(string topic, IEnumerable<byte[]> messages)
        {
            _nsqdHttpClient.PublishMultiple(topic, messages);
        }

        /// <summary>Stops the nsqd publisher.</summary>
        public void Stop()
        {
            // no-op
        }
    }
}
