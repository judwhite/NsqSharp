using System;
using System.Collections.ObjectModel;

namespace NsqMon.Plugin.Interfaces
{
    /// <summary>Specifies an NSQ Cluster, a group of nsqd and nsqlookupd instances.</summary>
    public interface ICluster
    {
        /// <summary>Gets the cluster's user-specified unique identifier.</summary>
        /// <value>The cluster's user-specified unique identifier.</value>
        Guid Id { get; }

        /// <summary>Gets the cluster's display name.</summary>
        /// <value>The cluster's display name.</value>
        string DisplayName { get; }

        /// <summary>Gets the environments associated with this cluster.</summary>
        /// <returns>The environments associated with this cluster.</returns>
        Collection<IEnvironment> GetEnvironments();
    }
}
