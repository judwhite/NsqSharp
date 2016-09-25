using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace NsqSharp.Api
{
    /// <summary>An nsqlookupd HTTP client.</summary>
    public class NsqLookupdHttpClient : NsqHttpApi
    {
        /// <summary>Initializes a new instance of <see cref="NsqLookupdHttpClient" /> class.</summary>
        /// <param name="nsqlookupdHttpAddress">The nsqlookupd HTTP address, including port. Example: 127.0.0.1:4161</param>
        /// <param name="httpRequestTimeout">The HTTP request timeout.</param>
        public NsqLookupdHttpClient(string nsqlookupdHttpAddress, TimeSpan httpRequestTimeout)
            : base(nsqlookupdHttpAddress, httpRequestTimeout)
        {
        }

        // TODO: Finish implementations

        /// <summary>Returns a list of nsqd producers and channel information for a topic.</summary>
        /// <param name="topic">The topic to list producers for.</param>
        public NsqLookupdLookupResponse Lookup(string topic)
        {
            ValidateTopic(topic);

            var json = Get(string.Format("/lookup?topic={0}", topic));

            var serializer = new DataContractJsonSerializer(typeof(NsqLookupdLookupResponse));
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return ((NsqLookupdLookupResponse)serializer.ReadObject(memoryStream));
            }
        }

        /// <summary>Returns a list of all known topics.</summary>
        public string[] GetTopics()
        {
            var json = Get(string.Format("/topics"));

            var serializer = new DataContractJsonSerializer(typeof(NsqLookupdTopicsResponse));
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return ((NsqLookupdTopicsResponse)serializer.ReadObject(memoryStream)).Topics;
            }
        }

        /// <summary>Returns a list of all known channels of a <paramref name="topic"/>.</summary>
        /// <param name="topic">The topic to list channels for.</param>
        private void GetChannels(string topic)
        {
            ValidateTopic(topic);

            //var json = Get(string.Format("/channels?topic={0}", topic));
        }

        /// <summary>Returns a list of all known nsqd nodes.</summary>
        public ProducerInformation[] GetNodes()
        {
            var json = Get(string.Format("/nodes"));

            var serializer = new DataContractJsonSerializer(typeof(NsqLookupdNodesResponse));
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return ((NsqLookupdNodesResponse)serializer.ReadObject(memoryStream)).Producers;
            }
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

    /// <summary>nsqlookupd response to /topics.</summary>
    [DataContract]
    public class NsqLookupdTopicsResponse
    {
        /// <summary>Gets or sets the topics.</summary>
        /// <value>The topics.</value>
        [DataMember(Name = "topics")]
        public string[] Topics { get; set; }
    }

    /// <summary>nsqlookupd response from /nodes.</summary>
    [DataContract]
    public class NsqLookupdNodesResponse
    {
        /// <summary>Gets or sets the producers.</summary>
        /// <value>The producers.</value>
        [DataMember(Name = "producers")]
        public ProducerInformation[] Producers { get; set; }
    }

    /// <summary>nsqlookupd producer list from /nodes.</summary>
    [DataContract]
    public class ProducerInformation : TopicProducerInformation
    {
        /// <summary>Gets a value indicating if an entry in <see cref="Topics"/> is tombstoned.</summary>
        /// <value>The tombstones.</value>
        [DataMember(Name = "tombstones")]
        public bool[] Tombstones { get; set; }

        /// <summary>Gets or sets the topics.</summary>
        /// <value>The topics.</value>
        [DataMember(Name = "topics")]
        public string[] Topics { get; set; }
    }

    /// <summary>nsqlookupd response from /lookup?topic=[topic_name].</summary>
    [DataContract]
    public class NsqLookupdLookupResponse
    {
        /// <summary>Gets or sets the nodes producing the topic.</summary>
        /// <value>The nodes producing the topic.</value>
        [DataMember(Name = "producers")]
        public TopicProducerInformation[] Producers { get; set; }

        /// <summary>Gets or sets the channels associated with the topic</summary>
        /// <value>The channels associated with the topic.</value>
        [DataMember(Name = "channels")]
        private string[] Channels { get; set; }
    }

    /// <summary>nsqlookupd producer list from /lookup?topic=[topic_name].</summary>
    [DataContract]
    public class TopicProducerInformation
    {
        /// <summary>Gets or sets the remote address.</summary>
        /// <value>The remote address.</value>
        [DataMember(Name = "remote_address")]
        public string RemoteAddress { get; set; }

        /// <summary>Gets or sets the hostname.</summary>
        /// <value>The hostname.</value>
        [DataMember(Name = "hostname")]
        public string Hostname { get; set; }

        /// <summary>Gets or sets the broadcast address.</summary>
        /// <value>The broadcast address.</value>
        [DataMember(Name = "broadcast_address")]
        public string BroadcastAddress { get; set; }

        /// <summary>Gets or sets the TCP port.</summary>
        /// <value>The TCP port.</value>
        [DataMember(Name = "tcp_port")]
        public int TcpPort { get; set; }

        /// <summary>Gets or sets the HTTP port.</summary>
        /// <value>The HTTP port.</value>
        [DataMember(Name = "http_port")]
        public int HttpPort { get; set; }

        /// <summary>Gets or sets the nsqd version.</summary>
        /// <value>The nsqd version.</value>
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
