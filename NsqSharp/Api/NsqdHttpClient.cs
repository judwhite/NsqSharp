using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using NsqSharp.Utils;

namespace NsqSharp.Api
{
    /// <summary>An nsqd HTTP client.</summary>
    public class NsqdHttpClient : NsqHttpApi
    {
        private readonly int _timeoutMilliseconds;

        /// <summary>Initializes a new instance of <see cref="NsqLookupdHttpClient" /> class.</summary>
        /// <param name="nsqdHttpAddress">The nsqlookupd HTTP address.</param>
        /// <param name="httpRequestTimeout">The HTTP request timeout.</param>
        public NsqdHttpClient(string nsqdHttpAddress, TimeSpan httpRequestTimeout)
            : base(nsqdHttpAddress, httpRequestTimeout)
        {
            _timeoutMilliseconds = (int)httpRequestTimeout.TotalMilliseconds;
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string Publish(string topic, string message)
        {
            ValidateTopic(topic);
            if (message == null)
                throw new ArgumentNullException("message");

            return Publish(topic, Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string Publish(string topic, byte[] message)
        {
            ValidateTopic(topic);
            if (message == null)
                throw new ArgumentNullException("message");

            string route = string.Format("/pub?topic={0}", topic);
            return Post(route, message);
        }

        /// <summary>
        /// Publishes multiple messages. More efficient than calling Publish several times for the same message type.
        /// See http://nsq.io/components/nsqd.html#mpub.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string PublishMultiple(string topic, IEnumerable<string> messages)
        {
            ValidateTopic(topic);
            if (messages == null)
                throw new ArgumentNullException("messages");

            var messagesArray = messages.ToArray();
            if (messagesArray.Length == 0)
                return null;

            string body = string.Join("\n", messagesArray);

            string route = string.Format("/mpub?topic={0}", topic);
            return Post(route, Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Publishes multiple messages. More efficient than calling Publish several times for the same message type.
        /// See http://nsq.io/components/nsqd.html#mpub.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public string PublishMultiple(string topic, IEnumerable<byte[]> messages)
        {
            ValidateTopic(topic);
            if (messages == null)
                throw new ArgumentNullException("messages");

            ICollection<byte[]> msgList = messages as ICollection<byte[]> ?? messages.ToList();

            if (msgList.Count == 0)
                return null;

            byte[] body;
            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    Binary.BigEndian.PutUint32(binaryWriter, msgList.Count);

                    foreach (var msg in msgList)
                    {
                        Binary.BigEndian.PutUint32(binaryWriter, msg.Length);
                        binaryWriter.Write(msg);
                    }
                }

                body = memoryStream.ToArray();
            }

            string route = string.Format("/mpub?topic={0}&binary=true", topic);
            return Post(route, body);
        }

        /// <summary>
        /// Empty a topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string EmptyTopic(string topic)
        {
            ValidateTopic(topic);

            string route = string.Format("/topic/empty?topic={0}", topic);
            return Post(route);
        }

        /// <summary>
        /// Empty a channel.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string EmptyChannel(string topic, string channel)
        {
            ValidateTopicAndChannel(topic, channel);

            string route = string.Format("/channel/empty?topic={0}&channel={1}", topic, channel);
            return Post(route);
        }

        /// <summary>
        /// Pause a topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string PauseTopic(string topic)
        {
            ValidateTopic(topic);

            string route = string.Format("/topic/pause?topic={0}", topic);
            return Post(route);
        }

        /// <summary>
        /// Unpause a topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string UnpauseTopic(string topic)
        {
            ValidateTopic(topic);

            string route = string.Format("/topic/unpause?topic={0}", topic);
            return Post(route);
        }

        /// <summary>
        /// Pause a channel.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string PauseChannel(string topic, string channel)
        {
            ValidateTopicAndChannel(topic, channel);

            string route = string.Format("/channel/pause?topic={0}&channel={1}", topic, channel);
            return Post(route);
        }

        /// <summary>
        /// Unpause a channel.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public string UnpauseChannel(string topic, string channel)
        {
            ValidateTopicAndChannel(topic, channel);

            string route = string.Format("/channel/unpause?topic={0}&channel={1}", topic, channel);
            return Post(route);
        }

        /// <summary>
        /// Returns internal instrumented statistics.
        /// </summary>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public NsqdStats GetStats()
        {
            string endpoint = GetFullUrl("/stats?format=json");
            byte[] respBody = Request(endpoint, HttpMethod.Get, _timeoutMilliseconds);

            var serializer = new DataContractJsonSerializer(typeof(NsqdStats));
            using (var memoryStream = new MemoryStream(respBody))
            {
                return ((NsqdStats)serializer.ReadObject(memoryStream));
            }
        }
    }

    /// <summary>
    /// Statistics information for nsqd. See <see cref="NsqdHttpClient.GetStats"/>.
    /// </summary>
    [DataContract]
    public class NsqdStats
    {
        ///<summary>version</summary>
        [DataMember(Name = "version")]
        public string Version { get; set; }
        ///<summary>health</summary>
        [DataMember(Name = "health")]
        public string Health { get; set; }
        ///<summary>topics</summary>
        [DataMember(Name = "topics")]
        public NsqdStatsTopic[] Topics { get; set; }
    }

    /// <summary>
    /// Topic information for nsqd. See <see cref="NsqdHttpClient.GetStats"/>.
    /// </summary>
    [DataContract]
    public class NsqdStatsTopic
    {
        ///<summary>topic_name</summary>
        [DataMember(Name = "topic_name")]
        public string TopicName { get; set; }
        ///<summary>channels</summary>
        [DataMember(Name = "channels")]
        public NsqdStatsChannel[] Channels { get; set; }
        ///<summary>depth</summary>
        [DataMember(Name = "depth")]
        public int Depth { get; set; }
        ///<summary>backend_depth</summary>
        [DataMember(Name = "backend_depth")]
        public int BackendDepth { get; set; }
        ///<summary>message_count</summary>
        [DataMember(Name = "message_count")]
        public int MessageCount { get; set; }
        ///<summary>paused</summary>
        [DataMember(Name = "paused")]
        public bool Paused { get; set; }
        ///<summary>e2e_processing_latency</summary>
        [DataMember(Name = "e2e_processing_latency")]
        public NsqdStatsEndToEndProcessingLatency EndToEndProcessingLatency { get; set; }
    }

    /// <summary>
    /// Channel information for nsqd. See <see cref="NsqdHttpClient.GetStats"/>.
    /// </summary>
    [DataContract]
    public class NsqdStatsChannel
    {
        ///<summary>channel_name</summary>
        [DataMember(Name = "channel_name")]
        public string ChannelName { get; set; }
        ///<summary>depth</summary>
        [DataMember(Name = "depth")]
        public int Depth { get; set; }
        ///<summary>backend_depth</summary>
        [DataMember(Name = "backend_depth")]
        public int BackendDepth { get; set; }
        ///<summary>in_flight_count</summary>
        [DataMember(Name = "in_flight_count")]
        public int InFlightCount { get; set; }
        ///<summary>deferred_count</summary>
        [DataMember(Name = "deferred_count")]
        public int DeferredCount { get; set; }
        ///<summary>message_count</summary>
        [DataMember(Name = "message_count")]
        public int MessageCount { get; set; }
        ///<summary>requeue_count</summary>
        [DataMember(Name = "requeue_count")]
        public int RequeueCount { get; set; }
        ///<summary>timeout_count</summary>
        [DataMember(Name = "timeout_count")]
        public int TimeoutCount { get; set; }
        ///<summary>clients</summary>
        [DataMember(Name = "clients")]
        public NsqdStatsClient[] Clients { get; set; }
        ///<summary>paused</summary>
        [DataMember(Name = "paused")]
        public bool Paused { get; set; }
        // TODO
        //[DataMember(Name = "e2e_processing_latency")]
        //public NsqdStatsEndToEndProcessingLatency EndToEndProcessingLatency { get; set; }
    }

    /// <summary>
    /// Client information for nsqd. See <see cref="NsqdHttpClient.GetStats"/>.
    /// </summary>
    [DataContract]
    public class NsqdStatsClient
    {
        ///<summary>name</summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }
        ///<summary>client_id</summary>
        [DataMember(Name = "client_id")]
        public string ClientId { get; set; }
        ///<summary>hostname</summary>
        [DataMember(Name = "hostname")]
        public string Hostname { get; set; }
        ///<summary>version</summary>
        [DataMember(Name = "version")]
        public string Version { get; set; }
        ///<summary>remote_address</summary>
        [DataMember(Name = "remote_address")]
        public string RemoteAddress { get; set; }
        ///<summary>state</summary>
        [DataMember(Name = "state")]
        public int State { get; set; }
        ///<summary>ready_count</summary>
        [DataMember(Name = "ready_count")]
        public int ReadyCount { get; set; }
        ///<summary>in_flight_count</summary>
        [DataMember(Name = "in_flight_count")]
        public int InFlightCount { get; set; }
        ///<summary>message_count</summary>
        [DataMember(Name = "message_count")]
        public int MessageCount { get; set; }
        ///<summary>finish_count</summary>
        [DataMember(Name = "finish_count")]
        public int FinishCount { get; set; }
        ///<summary>requeue_count</summary>
        [DataMember(Name = "requeue_count")]
        public int RequeueCount { get; set; }
        ///<summary>connect_ts</summary>
        [DataMember(Name = "connect_ts")]
        public int ConnectTimestamp { get; set; }
        ///<summary>sample_rate</summary>
        [DataMember(Name = "sample_rate")]
        public int SampleRate { get; set; }
        ///<summary>deflate</summary>
        [DataMember(Name = "deflate")]
        public bool Deflate { get; set; }
        ///<summary>snappy</summary>
        [DataMember(Name = "snappy")]
        public bool Snappy { get; set; }
        ///<summary>user_agent</summary>
        [DataMember(Name = "user_agent")]
        public string UserAgent { get; set; }
        ///<summary>tls</summary>
        [DataMember(Name = "tls")]
        public bool Tls { get; set; }
        ///<summary>tls_cipher_suite</summary>
        [DataMember(Name = "tls_cipher_suite")]
        public string TlsCipherSuite { get; set; }
        ///<summary>tls_version</summary>
        [DataMember(Name = "tls_version")]
        public string TlsVersion { get; set; }
        ///<summary>tls_negotiated_protocol</summary>
        [DataMember(Name = "tls_negotiated_protocol")]
        public string TlsNegotiatedProtocol { get; set; }
        ///<summary>tls_negotiated_protocol_is_mutual</summary>
        [DataMember(Name = "tls_negotiated_protocol_is_mutual")]
        public bool TlsNegotiatedProtocolIsMutual { get; set; }
    }

    /// <summary></summary>
    [DataContract]
    public class NsqdStatsEndToEndProcessingLatency
    {
        /// <summary>count</summary>
        [DataMember(Name = "count")]
        public int Count { get; set; }
        /// <summary>percentiles</summary>
        [DataMember(Name = "percentiles")]
        public NsqdStatsEndToEndProcessingLatencyPercentile[] Percentiles { get; set; }
    }

    /// <summary></summary>
    [DataContract]
    public class NsqdStatsEndToEndProcessingLatencyPercentile
    {
        /// <summary>quantile</summary>
        [DataMember(Name = "quantile")]
        public double Quantile { get; set; }
        /// <summary>value</summary>
        [DataMember(Name = "value")]
        public long Value { get; set; }
        /// <summary>time</summary>
        public TimeSpan Time
        {
            get { return TimeSpan.FromSeconds(Value / 1000000000.0); }
        }
    }
}
