using System;
using System.Collections.Generic;
using System.Net;
using PointOfSale.Common.Config;

namespace PointOfSale.Common.Utils
{
    public interface IRestClient
    {
        string Get(string endpoint);
    }

    internal class RestClient : IRestClient
    {
        private readonly IAppSettings _appSettings;
        private readonly INemesis _nemesis;

        private readonly Dictionary<string, string> _endpointResults = new Dictionary<string, string>();
        private readonly object _endpointResultsLocker = new object();

        public RestClient(IAppSettings appSettings, INemesis nemesis)
        {
            if (appSettings == null)
                throw new ArgumentNullException("appSettings");
            if (nemesis == null)
                throw new ArgumentNullException("nemesis");

            _appSettings = appSettings;
            _nemesis = nemesis;
        }

        public string Get(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException("endpoint");

            _nemesis.Invoke();

            if (_appSettings.UseServiceCallCache)
            {
                lock (_endpointResultsLocker)
                {
                    string cachedResponse;
                    if (_endpointResults.TryGetValue(endpoint, out cachedResponse))
                        return cachedResponse;
                }
            }

            var webClient = new WebClient();
            string response = webClient.DownloadString(endpoint);

            if (_appSettings.UseServiceCallCache)
            {
                lock (_endpointResultsLocker)
                {
                    _endpointResults[endpoint] = response;
                }
            }

            return response;
        }
    }
}
