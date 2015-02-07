using System;

namespace NsqSharp.Go
{
    /// <summary>
    /// ITcpConn interface. http://golang.org/pkg/net/#TCPConn
    /// </summary>
    public interface ITcpConn : IConn
    {
        /// <summary>
        /// CloseRead shuts down the reading side of the TCP connection. Most callers should just use Close.
        /// </summary>
        void CloseRead();

        /// <summary>
        /// CloseWrite shuts down the writing side of the TCP connection. Most callers should just use Close.
        /// </summary>
        void CloseWrite();

        /// <summary>
        /// Gets or sets the read timeout.
        /// </summary>
        TimeSpan ReadTimeout { get; set; }

        /// <summary>
        /// Gets or sets the write timeout.
        /// </summary>
        TimeSpan WriteTimeout { get; set; }
    }
}
