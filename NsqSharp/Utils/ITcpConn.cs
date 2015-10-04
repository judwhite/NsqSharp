using System;

namespace NsqSharp.Utils
{
    /// <summary>
    /// ITcpConn interface. http://golang.org/pkg/net/#TCPConn
    /// </summary>
    public interface ITcpConn : IConn
    {
        /// <summary>
        /// Gets or sets the read timeout.
        /// </summary>
        TimeSpan ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        TimeSpan WriteTimeout { get; set; }

        /// <summary>
        /// Flush writes all buffered data to the underlying TCP connection
        /// </summary>
        void Flush();
    }
}
