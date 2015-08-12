using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace NsqSharp.Core
{
    // https://github.com/bitly/go-nsq/blob/master/api_request.go

    // NOTE: deadlinedConn from the original go source is a timeout
    // on the http request and reading the response off the wire.
    //
    // to avoid convulted code a trade off has been made to only
    // consider time to first byte under the advisement of the
    // go-nsq team.

    [DataContract]
    internal class NsqLookupdApiResponse : INsqLookupdApiResponseProducers
    {
        [DataMember(Name = "data")]
        public NsqLookupdApiResponseData data { get; set; }
        [DataMember(Name = "producers")]
        public NsqLookupdApiResponseProducer[] producers { get; set; }
    }

    [DataContract]
    internal class NsqLookupdApiResponseData : INsqLookupdApiResponseProducers
    {
        [DataMember(Name = "producers")]
        public NsqLookupdApiResponseProducer[] producers { get; set; }
    }

    [DataContract]
    internal class NsqLookupdApiResponseProducer
    {
        [DataMember(Name = "broadcast_address")]
        public string broadcast_address { get; set; }
        [DataMember(Name = "http_port")]
        public int http_port { get; set; }
        [DataMember(Name = "tcp_port")]
        public int tcp_port { get; set; }
    }

    internal interface INsqLookupdApiResponseProducers
    {
        NsqLookupdApiResponseProducer[] producers { get; set; }
    }

    internal static class ApiRequest
    {
        public static INsqLookupdApiResponseProducers NegotiateV1(string method, string endpoint, TimeSpan timeout)
        {
            int timeoutMilliseconds = (int)timeout.TotalMilliseconds;
            if (timeoutMilliseconds < 2000)
                timeoutMilliseconds = 2000;

            var httpclient = (HttpWebRequest)WebRequest.Create(endpoint);
            httpclient.Proxy = WebRequest.DefaultWebProxy;
            httpclient.Method = method;
            httpclient.Timeout = timeoutMilliseconds;
            httpclient.Accept = "application/vnd.nsq; version=1.0";

            byte[] respBody;

            bool isNsqv1 = false;

            using (var response = (HttpWebResponse)httpclient.GetResponse())
            using (var responseStream = response.GetResponseStream())
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

                    respBody = memoryStream.ToArray();
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("got response {0} {1}",
                        response.StatusDescription, Encoding.UTF8.GetString(respBody)));
                }

                if (response.Headers.Get("X-NSQ-Content-Type") == "nsq; version=1.0")
                {
                    isNsqv1 = true;
                }
            }

            if (respBody.Length == 0)
            {
                respBody = Encoding.UTF8.GetBytes(@"{}");
            }

            //string json = Encoding.UTF8.GetString(respBody);
            var serializer = new DataContractJsonSerializer(typeof(NsqLookupdApiResponse));
            using (var memoryStream = new MemoryStream(respBody))
            {
                var apiResponse = (NsqLookupdApiResponse)serializer.ReadObject(memoryStream);
                if (isNsqv1)
                {
                    return apiResponse;
                }
                return apiResponse.data;
            }
        }
    }
}
