using System.Collections.ObjectModel;
using NsqMon.Plugin.Interfaces;

namespace NsqMon.Plugin
{
    /// <summary>Implement this interface to create an NsqMon plugin.</summary>
    public interface INsqMonPlugin
    {
        /// <summary>Gets the clusters provided by this plugin.</summary>
        /// <returns>The clusters provided by this plugin.</returns>
        Collection<ICluster> GetClusters();
    }
}