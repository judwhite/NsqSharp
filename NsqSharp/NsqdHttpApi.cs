using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NsqSharp.Go;

namespace NsqSharp
{
    /// <summary>
    /// NSQD HTTP API
    /// </summary>
    public static class NsqdHttpApi
    {
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
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
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="message">The message.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
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
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string PublishMultiple(string nsqdHttpAddress, string topic, IEnumerable<string> messages)
        {
            CheckArguments(nsqdHttpAddress, topic);
            if (messages == null)
                throw new ArgumentNullException("messages");

            string body = string.Join("\n", messages);

            return Post(GetEndpoint(nsqdHttpAddress, string.Format("/mpub?topic={0}", topic)), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Publishes multiple messages. More efficient than calling Publish several times for the same message type.
        /// See http://nsq.io/components/nsqd.html#mpub.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="messages">The messages.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string PublishMultiple(string nsqdHttpAddress, string topic, IEnumerable<byte[]> messages)
        {
            CheckArguments(nsqdHttpAddress, topic);
            if (messages == null)
                throw new ArgumentNullException("messages");

            ICollection<byte[]> msgList = messages as ICollection<byte[]> ?? messages.ToList();

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
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string CreateTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string body = string.Format("topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, "/topic/create"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Delete a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string DeleteTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string body = string.Format("topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, "/topic/delete"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Create a channel. Channel creation happens automatically on subscribe, use this method to pre-create a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string CreateChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string body = string.Format("topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, "/channel/create"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Delete a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string DeleteChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string body = string.Format("topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, "/channel/delete"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Empty a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string EmptyTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string body = string.Format("topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, "/topic/empty"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Empty a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string EmptyChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string body = string.Format("topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, "/channel/empty"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Pause a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string PauseTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string body = string.Format("topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, "/topic/pause"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Unpause a topic.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string UnpauseTopic(string nsqdHttpAddress, string topic)
        {
            CheckArguments(nsqdHttpAddress, topic);

            string body = string.Format("topic={0}", topic);
            return Post(GetEndpoint(nsqdHttpAddress, "/topic/unpause"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Pause a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string PauseChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string body = string.Format("topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, "/channel/pause"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Unpause a channel.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string UnpauseChannel(string nsqdHttpAddress, string topic, string channel)
        {
            CheckArguments(nsqdHttpAddress, topic, channel);

            string body = string.Format("topic={0}&channel={1}", topic, channel);
            return Post(GetEndpoint(nsqdHttpAddress, "/channel/unpause"), Encoding.UTF8.GetBytes(body));
        }

        /// <summary>
        /// Returns internal instrumented statistics.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
        public static string Stats(string nsqdHttpAddress)
        {
            if (string.IsNullOrEmpty(nsqdHttpAddress))
                throw new ArgumentNullException("nsqdHttpAddress");

            return Get(GetEndpoint(nsqdHttpAddress, "/stats?format=json"));
        }

        /// <summary>
        /// Returns version information.
        /// </summary>
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
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
        /// <param name="nsqdHttpAddress">The NSQD HTTP address.</param>
        /// <returns>The response from the NSQD HTTP server.</returns>
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
                nsqdHttpAddress = "http://";
            nsqdHttpAddress = nsqdHttpAddress.TrimEnd(new[] { '/' });
            route = route.TrimStart(new[] { '/' });

            return string.Format("{0}/{1}", nsqdHttpAddress, route);
        }

        private static string Post(string endpoint, byte[] body)
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
            webRequest.Accept = "application/vnd.nsq; version=1.0";
            webRequest.UserAgent = string.Format("{0}/{1} {2} {3}\\{4}", ClientInfo.ClientName, ClientInfo.Version,
                Environment.MachineName, Environment.UserDomainName, Environment.UserName);

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
}
