using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/api_request.go

    // NOTE: deadlinedConn from the original go source is a timeout
    // on the http request and reading the response off the wire.
    //
    // to avoid convulted code a trade off has been made to only
    // consider time to first byte under the advisement of the
    // go-nsq team.

    internal static class ApiRequest
    {
        public static JObject NegotiateV1(string method, string endpoint)
        {
            const int timeoutMilliseconds = 2000;

            var httpclient = WebRequest.CreateHttp(endpoint);
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

            string json = Encoding.UTF8.GetString(respBody);
            var data = JToken.Parse(json);
            if (isNsqv1)
            {
                return (JObject)data;
            }
            return data["data"].Value<JObject>();
        }
    }
}
