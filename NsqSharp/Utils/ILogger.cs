namespace NsqSharp.Utils
{
    /// <summary>
    /// A Logger represents an active logging object that generates lines of
    /// output to an io.Writer. Each logging operation makes a single call to
    /// the Writer's Write method. A Logger can be used simultaneously from
    /// multiple goroutines; it guarantees to serialize access to the Writer.
    /// https://godoc.org/log#Logger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Output writes the output for a logging event. The string s contains
        /// the text to print after the prefix specified by the flags of the
        /// Logger. A newline is appended if the last character of s is not
        /// already a newline.
        /// </summary>
        void Output(string s);
    }
}
