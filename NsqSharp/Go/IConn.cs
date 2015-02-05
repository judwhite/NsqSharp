namespace NsqSharp.Go
{
    /// <summary>
    /// IConn interface. http://golang.org/pkg/net/#Conn
    /// </summary>
    public interface IConn : IReader, IWriter
    {
        /// <summary>
        /// Close closes the connection.
        /// </summary>
        void Close();
    }
}
