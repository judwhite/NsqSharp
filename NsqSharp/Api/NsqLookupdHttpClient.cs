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
    }
}
