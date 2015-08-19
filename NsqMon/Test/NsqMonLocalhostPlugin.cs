using System;
using System.Collections.ObjectModel;
using NsqMon.Plugin;
using NsqMon.Plugin.Interfaces;

namespace NsqMon.Test
{
    public class NsqMonLocalhostPlugin : INsqMonPlugin
    {
        private Collection<ICluster> _clusters;

        /// <summary>Gets the clusters provided by this plugin.</summary>
        /// <returns>The clusters provided by this plugin.</returns>
        public Collection<ICluster> GetClusters()
        {
            if (_clusters == null)
            {
                _clusters = new Collection<ICluster>();

                var localhostCluster = new NsqCluster(
                    Guid.Parse("E48FFAC9-C276-457D-9725-7BC225066240"),
                    "NsqMon Localhost Cluster",
                    new[]
                    {
                        new NsqEnvironment(
                            Guid.Parse("C97BEEF3-BC75-42DE-A886-CCB7D81D0760"),
                            "localhost",
                            new Collection<Uri>(new[] {new Uri("http://127.0.0.1:4161")})
                        )
                    }
                );

                _clusters.Add(localhostCluster);
            }

            return _clusters;
        }
    }
}
