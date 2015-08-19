using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NsqMon.Plugin.Interfaces;

namespace NsqMon.Test
{
    public class NsqEnvironment : IEnvironment
    {
        private readonly Collection<Uri> _nsqLookupds;

        public NsqEnvironment(Guid id, string displayName, IList<Uri> nsqLookupds)
        {
            Id = id;
            DisplayName = displayName;
            _nsqLookupds = new Collection<Uri>(nsqLookupds);
        }

        /// <summary>Gets the environment's display user-specified unique identifier.</summary>
        /// <value>The environment's display user-specified unique identifier.</value>
        public Guid Id { get; private set; }

        /// <summary>Gets the environment's display name.</summary>
        /// <value>The environment's display name.</value>
        public string DisplayName { get; private set; }

        /// <summary>Gets nsqlookupd instances associated with this environment.</summary>
        /// <returns>The nsqlookupd instances associated with this environment.</returns>
        public Collection<Uri> GetNsqLookupds()
        {
            return _nsqLookupds;
        }
    }
}
