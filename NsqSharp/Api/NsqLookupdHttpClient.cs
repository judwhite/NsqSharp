using System;

namespace NsqSharp.Api
{
    /// <summary>An nsqlookupd HTTP client.</summary>
    public class NsqLookupdHttpClient : NsqHttpApi
    {
        /// <summary>Initializes a new instance of <see cref="NsqLookupdHttpClient" /> class.</summary>
        /// <param name="nsqlookupdHttpAddress">The nsqlookupd HTTP address.</param>
        /// <param name="httpRequestTimeout">The HTTP request timeout.</param>
        public NsqLookupdHttpClient(string nsqlookupdHttpAddress, TimeSpan httpRequestTimeout)
            : base(nsqlookupdHttpAddress, httpRequestTimeout)
        {
        }

        // TODO: Finish implementations

        /// <summary>Returns a list of nsqd producers for a topic.</summary>
        /// <param name="topic">The topic to list producers for.</param>
        private void GetLookup(string topic)
        {
            ValidateTopic(topic);

            //var json = Get(string.Format("/lookup?topic={0}", topic));
        }

        /// <summary>Returns a list of all known topics.</summary>
        private string[] GetTopics()
        {
            //var json = Get(string.Format("/topics"));
            return new[] { "TODO" };
        }

        /// <summary>Returns a list of all known channels of a <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic to list channels for.</param>
        private void GetChannels(string topic)
        {
            ValidateTopic(topic);

            //var json = Get(string.Format("/channels?topic={0}", topic));
        }

        /// <summary>Returns a list of all known nsqd nodes.</summary>
        private void GetNodes()
        {
            //var json = Get(string.Format("/nodes"));
        }

        /// <summary>
        ///     Tombstones a specific <paramref name="nsqdNode"/> producer of an existing <paramref name="topic"/>.
        ///     Tombstoning a <paramref name="topic"/>
        ///     prevents clients from discovering the <paramref name="nsqdNode"/> through nsqlookupd for a configurable --
        ///     tombstone-liftime, allowing the <paramref name="nsqdNode"/>
        ///     to delete the topic without it being recreated by connecting clients.
        /// </summary>
        /// <param name="topic">The topic to list producers for.</param>
        /// <param name="nsqdNode">The nsqd node.</param>
        private void TombstoneTopicProducer(string topic, string nsqdNode)
        {
            if (string.IsNullOrEmpty(nsqdNode))
                throw new ArgumentNullException("nsqdNode");

            ValidateTopic(topic);

            //var json = Get(string.Format("/tombstone_topic_producer?topic={0}&node={1}", topic, nsqdNode));
        }
    }
}
