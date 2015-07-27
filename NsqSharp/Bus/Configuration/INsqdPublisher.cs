using System.Collections.Generic;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>Interface for an NSQD publisher.</summary>
    public interface INsqdPublisher
    {
        /// <summary>Publishes a <paramref name="message"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        void Publish(string topic, byte[] message);

        /// <summary>Multi-Publishes <paramref name="messages"/> on the specified <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        void MultiPublish(string topic, IEnumerable<byte[]> messages);

        /// <summary>Stops the nsqd publisher.</summary>
        void Stop();
    }
}
