using System;
using System.Collections.ObjectModel;

namespace NsqMon.Plugin.Interfaces
{
    /// <summary>
    ///     Specifies an environment, such as localhost, staging, or production. Each environment uniquely belongs to a
    ///     cluster.
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>Gets the environment's display user-specified unique identifier.</summary>
        /// <value>The environment's display user-specified unique identifier.</value>
        Guid Id { get; }

        /// <summary>Gets the environment's display name.</summary>
        /// <value>The environment's display name.</value>
        string DisplayName { get; }

        /// <summary>Gets nsqlookupd instances associated with this environment.</summary>
        /// <returns>The nsqlookupd instances associated with this environment.</returns>
        Collection<Uri> GetNsqLookupds();
    }
}
