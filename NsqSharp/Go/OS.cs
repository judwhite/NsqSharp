using System.Net;

namespace NsqSharp.Go
{
    /// <summary>
    /// OS Package. http://golang.org/pkg/os
    /// </summary>
    public static class OS
    {
        /// <summary>
        /// Returns the host name
        /// </summary>
        public static string Hostname()
        {
            return Dns.GetHostEntry("localhost").HostName;
        }
    }
}
