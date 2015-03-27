using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using NsqSharp.Core;
using NsqSharp.Utils;

namespace NsqSharp
{
    /// <summary>
    /// HTTP API for interacting with nsqd. See http://nsq.io/components/nsqd.html#pub.
    /// </summary>
    public static class NsqdHttpApi
    {
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string Publish(string nsqdHttpAddress, string topic, string message)
        {
            CheckArguments(nsqdHttpAddress, topic);
            if (message == null)
                throw new ArgumentNullException("message");

            return Publish(nsqdHttpAddress, topic, Encoding.UTF8.GetBytes(message));
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string Publish(string nsqdHttpAddress, string topic, byte[] message)
        {
            CheckArguments(nsqdHttpAddress, topic);
            if (message == null)
                throw new ArgumentNullException("message");

            return Post(GetEndpoint(nsqdHttpAddress, string.Format("/pub?topic={0}", topic)), message);
        }

        /// <summary>
        /// Publishes multiple messages. More efficient than calling Publish several times for the same message type.
        /// See http://nsq.io/components/nsqd.html#mpub.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string PublishMultiple(string nsqdHttpAddress, string topic, IEnumerable<string> messages)
        {
            CheckArguments(nsqdHttpAddress, topic);
            if (messages == null)
                throw new ArgumentNullException("messages");

            var messagesArray = messages.ToArray();
            if (messagesArray.Length == 0)
                return null;

            string body = string.Join("\n", messagesArray);

            return Post(GetEndpoint(nsqdHttpAddress, string.Format("/mpub?topic={0}", topic)), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Publishes multiple messages. More efficient than calling Publish several times for the same message type.
        /// See http://nsq.io/components/nsqd.html#mpub.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string PublishMultiple(string nsqdHttpAddress, string topic, IEnumerable<byte[]> messages)
        {
            CheckArguments(nsqdHttpAddress, topic);
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

            return Post(GetEndpoint(nsqdHttpAddress, string.Format("/mpub?topic={0}&binary=true", topic)), body);
        }

        /// <summary>
        /// Create a topic. Topic creation happens automatically on publish, use this method to pre-create a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string CreateTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string route = string.Format("/topic/create?topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Delete a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string DeleteTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string route = string.Format("/topic/delete?topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Create a channel. Channel creation happens automatically on subscribe, use this method to pre-create a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string CreateChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string route = string.Format("/channel/create?topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Delete a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string DeleteChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string route = string.Format("/channel/delete?topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Empty a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string EmptyTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string route = string.Format("/topic/empty?topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Empty a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string EmptyChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string route = string.Format("/channel/empty?topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Pause a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string PauseTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string route = string.Format("/topic/pause?topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Unpause a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string UnpauseTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string route = string.Format("topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Pause a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string PauseChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string route = string.Format("/channel/pause?topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Unpause a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string UnpauseChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string route = string.Format("/channel/unpause?topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, route));
        }

        /// <summary>
        /// Returns internal instrumented statistics.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static NsqdStats Stats(string nsqdHttpAddress)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");

            string response = Get(GetEndpoint(nsqdHttpAddress, "/stats?format=json"));
            byte[] respBody = Encoding.UTF8.GetBytes(response);

            var serializer = new DataContractJsonSerializer(typeof(NsqdStatsResponse));
            using (var memoryStream = new MemoryStream(respBody))
            {
                return ((NsqdStatsResponse)serializer.ReadObject(memoryStream)).Data;
            }
        }

        /// <summary>
        /// Returns version information.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string Info(string nsqdHttpAddress)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");

            return Get(GetEndpoint(nsqdHttpAddress, "/info"));
        }

        /// <summary>
        /// Monitoring endpoint, should return OK. It returns a 500 if it is not healthy. At the moment, the only unhealthy
        /// state would be if it failed to write messages to disk when overflow occurred.
        /// </summary>
        /// <param name="nsqdHttpAddress">The nsqd HTTP address.</param>
        /// <returns>The response from the nsqd HTTP server.</returns>
        public static string Ping(string nsqdHttpAddress)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");

            return Get(GetEndpoint(nsqdHttpAddress, "/ping"));
        }

        private static void CheckArguments(string nsqdHttpAddress, string topic)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");
            if (!Protocol.IsValidTopicName(topic))
                throw new ArgumentException(string.Format("'{0}' is an invalid topic name", topic), "topic");
        }

        private static void CheckArguments(string nsqdHttpAddress, string topic, string channel)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException("channel");
            if (!Protocol.IsValidTopicName(topic))
                throw new ArgumentException(string.Format("'{0}' is an invalid topic name", topic), "topic");
            if (!Protocol.IsValidTopicName(channel))
                throw new ArgumentException(string.Format("'{0}' is an invalid channel name", channel), "channel");
        }

        private static string GetEndpoint(string nsqdHttpAddress, string route)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException("route");

            if (!nsqdHttpAddress.StartsWith("http"))
                nsqdHttpAddress = "http://" + nsqdHttpAddress;
            nsqdHttpAddress = nsqdHttpAddress.TrimEnd(new[] { '/' });
            route = route.TrimStart(new[] { '/' });

            return string.Format("{0}/{1}", nsqdHttpAddress, route);
        }

        private static string Post(string endpoint, byte[] body = null)
        {
            return Request(endpoint, "POST", body);
        }

        private static string Get(string endpoint)
        {
            return Request(endpoint, "GET");
        }

        private static string Request(string endpoint, string method, byte[] body = null)
        {
            const int timeoutMilliseconds = 2000;

            var webRequest = (HttpWebRequest)WebRequest.Create(endpoint);
            webRequest.Proxy = WebRequest.DefaultWebProxy;
            webRequest.Method = method;
            webRequest.Timeout = timeoutMilliseconds;
            webRequest.Accept = "text/html,application/vnd.nsq; version=1.0";
            webRequest.UserAgent = string.Format("{0}/{1}", ClientInfo.ClientName, ClientInfo.Version);

            if (method == "POST" && body != null && body.Length != 0)
            {
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = body.Length;

                using (var request = webRequest.GetRequestStream())
                {
                    request.Write(body, 0, body.Length);
                }
            }

            string response;

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse())
            using (var responseStream = httpResponse.GetResponseStream())
            {
                if (responseStream == null)
                    throw new Exception("responseStream is null");

                var buf = new byte[256];
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    int read;
                    do
                    {
                        read = responseStream.Read(buf, 0, 256);
                        memoryStream.Write(buf, 0, read);
                    } while (read > 0);

                    response = Encoding.UTF8.GetString(memoryStream.ToArray());
                }

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("got response {0} {1}", httpResponse.StatusDescription, response));
                }
            }

            return response;
        }
    }

    /// <summary>
    /// Statistics information response wrapper for nsqd. See <see cref="NsqdHttpApi.Stats"/>.
    /// </summary>
    [DataContract]
    public class NsqdStatsResponse
    {
        /// <summary>HTTP status code.</summary>
        [DataMember(Name = "status_code")]
        public string StatusCode { get; set; }
        /// <summary>HTTP status text.</summary>
        [DataMember(Name = "status_txt")]
        public string StatusText { get; set; }
        /// <summary>Statistics information for nsqd.</summary>
        [DataMember(Name = "data")]
        public NsqdStats Data { get; set; }
    }

    /// <summary>
    /// Statistics information for nsqd. See <see cref="NsqdHttpApi.Stats"/>.
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
    /// Topic information for nsqd. See <see cref="NsqdHttpApi.Stats"/>.
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
    /// Channel information for nsqd. See <see cref="NsqdHttpApi.Stats"/>.
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
    /// Client information for nsqd. See <see cref="NsqdHttpApi.Stats"/>.
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
