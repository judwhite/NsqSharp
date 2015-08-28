using System;
using System.IO;
using System.Net;
using System.Text;
using NsqSharp.Core;

namespace NsqSharp.Api
{
    /// <summary>
    /// HTTP client for interacting with the common API between nsqd and nsqlookupd. See http://nsq.io/components/nsqd.html#pub.
    /// See <see cref="NsqdHttpClient"/> and <see cref="NsqLookupdHttpClient"/>.
    /// </summary>
    public abstract class NsqHttpApi
    {
        private readonly string _httpAddress;
        private readonly int _timeoutMilliseconds;

        /// <summary>Initializes a new instance of <see cref="NsqHttpApi" /> class.</summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpAddress"/> is <c>null</c> or empty.
        /// </exception>
        /// <param name="httpAddress">The nsqd or nsqlookupd HTTP address.</param>
        /// <param name="httpRequestTimeout">The HTTP request timeout.</param>
        protected NsqHttpApi(string httpAddress, TimeSpan httpRequestTimeout)
        {
            if (string.IsNullOrEmpty(httpAddress))
                throw new ArgumentNullException("httpAddress");
            if (httpRequestTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("httpRequestTimeout", httpRequestTimeout,
                    "httpRequestTimeout must be greater than TimeSpan.Zero");

            if (!httpAddress.StartsWith("http"))
                httpAddress = "http://" + httpAddress;
            httpAddress = httpAddress.TrimEnd(new[] { '/' });

            _timeoutMilliseconds = (int)httpRequestTimeout.TotalMilliseconds;

            _httpAddress = httpAddress;
        }

        /// <summary>
        /// Create a topic. Topic creation happens automatically on publish, use this method to pre-create a topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the server.</returns>
        public string CreateTopic(string topic)
        {
            ValidateTopic(topic);

            string route = string.Format("/topic/create?topic={0}", topic);
            return Post(route);
        }

        /// <summary>
        /// Delete a topic.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <returns>The response from the server.</returns>
        public string DeleteTopic(string topic)
        {
            ValidateTopic(topic);

            string route = string.Format("/topic/delete?topic={0}", topic);
            return Post(route);
        }

        /// <summary>
        /// Create a channel. Channel creation happens automatically on subscribe, use this method to pre-create a channel.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the server.</returns>
        public string CreateChannel(string topic, string channel)
        {
            ValidateTopicAndChannel(topic, channel);

            string route = string.Format("/channel/create?topic={0}&channel={1}", topic, channel);
            return Post(route);
        }

        /// <summary>
        /// Delete a channel.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="channel">The channel.</param>
        /// <returns>The response from the server.</returns>
        public string DeleteChannel(string topic, string channel)
        {
            ValidateTopicAndChannel(topic, channel);

            string route = string.Format("/channel/delete?topic={0}&channel={1}", topic, channel);
            return Post(route);
        }

        /// <summary>
        /// Returns version information as a JSON string.
        /// </summary>
        /// <returns>The response from the server.</returns>
        public string GetInfo()
        {
            return Get("/info");
        }

        /// <summary>
        /// Monitoring endpoint, should return OK. It returns a 500 if it is not healthy. At the moment, the only unhealthy
        /// state would be if it failed to write messages to disk when overflow occurred.
        /// </summary>
        /// <returns>The response from the server.</returns>
        public string Ping()
        {
            return Get("/ping");
        }

        /// <summary>Validates the topic name.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        /// <param name="topic">The topic name.</param>
        protected static void ValidateTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentNullException("topic");
            if (!Protocol.IsValidTopicName(topic))
                throw new ArgumentException(string.Format("'{0}' is an invalid topic name", topic), "topic");
        }

        /// <summary>Validates the topic and channel name.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        /// <param name="topic">The topic name.</param>
        /// <param name="channel">The channel name.</param>
        protected static void ValidateTopicAndChannel(string topic, string channel)
        {
            ValidateTopic(topic);
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException("channel");
            if (!Protocol.IsValidTopicName(channel))
                throw new ArgumentException(string.Format("'{0}' is an invalid channel name", channel), "channel");
        }

        /// <summary>Gets the HTTP address plus route.</summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="route">The route.</param>
        /// <returns>The full URL.</returns>
        protected string GetFullUrl(string route)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException("route");

            route = route.TrimStart(new[] { '/' });

            return string.Format("{0}/{1}", _httpAddress, route);
        }

        /// <summary>POSTs to the specified route using the HTTP address from the constructor.</summary>
        /// <param name="route">The route.</param>
        /// <param name="body">The body.</param>
        /// <returns>The response from the server.</returns>
        protected string Post(string route, byte[] body = null)
        {
            string endpoint = GetFullUrl(route);
            var bytes = Request(endpoint, HttpMethod.Post, _timeoutMilliseconds, body);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>GETs from the specified route using the HTTP address from the constructor.</summary>
        /// <param name="route">The route.</param>
        /// <returns>The response from the server.</returns>
        protected string Get(string route)
        {
            string endpoint = GetFullUrl(route);
            var bytes = Request(endpoint, HttpMethod.Get, _timeoutMilliseconds);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>Initiates and HTTP request to the specified <paramref name="endpoint"/>.</summary>
        /// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="timeoutMilliseconds">The timeout in milliseconds.</param>
        /// <param name="body">The body.</param>
        /// <returns>The response from the server.</returns>
        protected static byte[] Request(string endpoint, HttpMethod httpMethod, int timeoutMilliseconds, byte[] body = null)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(endpoint);
            webRequest.Proxy = WebRequest.DefaultWebProxy;
            webRequest.Method = httpMethod == HttpMethod.Post ? "POST" : "GET";
            webRequest.Timeout = timeoutMilliseconds;
            webRequest.Accept = "application/vnd.nsq; version=1.0";
            webRequest.UserAgent = string.Format("{0}/{1}", ClientInfo.ClientName, ClientInfo.Version);

            if (httpMethod == HttpMethod.Post && body != null && body.Length != 0)
            {
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = body.Length;

                using (var request = webRequest.GetRequestStream())
                {
                    request.Write(body, 0, body.Length);
                }
            }

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse())
            using (var responseStream = httpResponse.GetResponseStream())
            {
                if (responseStream == null)
                    throw new Exception("responseStream is null");

                int contentLength = (int)httpResponse.ContentLength;
                byte[] responseBytes;

                var buf = new byte[256];
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    int bytesRead;
                    do
                    {
                        bytesRead = responseStream.Read(buf, 0, 256);
                        memoryStream.Write(buf, 0, bytesRead);
                    } while (bytesRead > 0);

                    responseBytes = memoryStream.ToArray();
                }

                if (responseBytes.Length < contentLength)
                    throw new Exception(string.Format("premature end of response stream {0}", endpoint));

                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("got response {0} {1} {2}",
                        httpResponse.StatusDescription, endpoint, Encoding.UTF8.GetString(responseBytes)));
                }

                return responseBytes;
            }
        }
    }

    /// <summary>Values that represent HTTP methods.</summary>
    public enum HttpMethod
    {
        /// <summary>GET method.</summary>
        Get,
        /// <summary>POST method.</summary>
        Post
    }
}
