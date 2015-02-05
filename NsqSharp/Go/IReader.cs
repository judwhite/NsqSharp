namespace NsqSharp.Go
{
    /// <summary>
    /// Reader is the interface that wraps the basic Read method. http://golang.org/pkg/io/#Reader
    /// </summary>
    public interface IReader
    {
        /// <summary>
        /// Read reads data from the connection.
        /// </summary>
        /// <param name="b">The byte array to populate.</param>
        /// <returns>The number of bytes read.</returns>
        int Read(byte[] b);
    }
}
