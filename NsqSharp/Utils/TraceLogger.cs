using System.Diagnostics;

namespace NsqSharp.Utils
{
    /// <summary>
    /// Trace logger
    /// </summary>
    public class TraceLogger : ILogger
    {
        /// Output writes the output for a logging event. The string s contains
        /// the text to print after the prefix specified by the flags of the
        /// Logger. A newline is appended if the last character of s is not
        /// already a newline.
        public void Output(string s)
        {
            Trace.WriteLine(s);
        }
    }
}
