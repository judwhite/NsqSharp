using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NsqMon.Plugin.Interfaces;

namespace NsqMon.Test
{
    public class NsqCluster : ICluster
    {
        private readonly Collection<IEnvironment> _environments;

        public NsqCluster(Guid id, string displayName, IList<IEnvironment> environments)
        {
            Id = id;
            DisplayName = displayName;
            _environments = new Collection<IEnvironment>(environments);
        }

        /// <summary>Gets the cluster's user-specified unique identifier.</summary>
        /// <value>The cluster's user-specified unique identifier.</value>
        public Guid Id { get; private set; }

        /// <summary>Gets the cluster's display name.</summary>
        /// <value>The cluster's display name.</value>
        public string DisplayName { get; }

        /// <summary>Gets the environments associated with this cluster.</summary>
        /// <returns>The environments associated with this cluster.</returns>
        public Collection<IEnvironment> GetEnvironments()
        {
            return _environments;
        }
    }
}
