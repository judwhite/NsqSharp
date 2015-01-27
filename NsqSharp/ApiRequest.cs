using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using NsqSharp.Channels;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/api_request.go

    internal static class ApiRequest
    {
        public static JObject NegotiateV1(string method, string endpoint)
        {
            int timeoutMilliseconds = 2000;

            var httpclient = WebRequest.CreateHttp(endpoint);
            httpclient.Proxy = WebRequest.DefaultWebProxy;
            httpclient.Method = method;
            httpclient.Timeout = timeoutMilliseconds;
            httpclient.Accept = "application/vnd.nsq; version=1.0";

            byte[] respBody = null;

            bool isNsqv1 = false;

            var start = DateTime.UtcNow;

            using (var response = (HttpWebResponse)httpclient.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                    throw new Exception("responseStream is null");

                timeoutMilliseconds -= (int)(DateTime.UtcNow - start).TotalMilliseconds;
                if (timeoutMilliseconds <= 0)
                    throw new TimeoutException(string.Format("timeout reading from {0}", endpoint));

                var timeout = Time.After(TimeSpan.FromMilliseconds(timeoutMilliseconds));
                var receive = new Chan<byte[]>();

                Thread t = new Thread(() =>
                {
                    var buf = new byte[256];
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        int read;
                        do
                        {
                            read = responseStream.Read(buf, 0, 256);
                            memoryStream.Write(buf, 0, read);
                        } while (read > 0);

                        buf = memoryStream.ToArray();
                    }

                    receive.Send(buf);
                });
                t.IsBackground = true;
                t.Start();

                Select
                    .CaseReceive(receive, b =>
                    {
                        respBody = b;
                    })
                    .CaseReceive(timeout, b =>
                    {
                        try
                        {
                            t.Abort();
                        }
                        catch
                        {
                        }

                        throw new TimeoutException(string.Format("timeout reading from {0}", endpoint));
                    })
                    .NoDefault();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(string.Format("got response {0} {1}", response.StatusDescription, Encoding.UTF8.GetString(respBody)));
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
