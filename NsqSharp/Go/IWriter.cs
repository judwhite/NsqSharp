namespace NsqSharp.Go
{
    /// <summary>
    /// Writer is the interface that wraps the basic Write method. http://golang.org/pkg/io/#Writer
    /// </summary>
    public interface IWriter
    {
        /// <summary>
        /// Write writes data to the connection.
        /// </summary>
        /// <returns>The number of bytes written.</returns>
        int Write(byte[] b, int offset, int length);
    }
}
