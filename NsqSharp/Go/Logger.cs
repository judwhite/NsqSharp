using System;
using System.Diagnostics;

namespace NsqSharp.Go
{
    /// <summary>
    /// A Logger represents an active logging object that generates lines of
    /// output to an io.Writer. Each logging operation makes a single call to
    /// the Writer's Write method. A Logger can be used simultaneously from
    /// multiple goroutines; it guarantees to serialize access to the Writer.
    /// https://godoc.org/log#Logger
    /// </summary>
    public class Logger : ILogger
    {
        // TODO

        /// Output writes the output for a logging event.  The string s contains
        /// the text to print after the prefix specified by the flags of the
        /// Logger.  A newline is appended if the last character of s is not
        /// already a newline.  Calldepth is used to recover the PC and is
        /// provided for generality, although at the moment on all pre-defined
        /// paths it will be 2.
        public void Output(int calldepth, string s)
        {
            Console.WriteLine(s);
            Debug.WriteLine(s);
        }
    }
}
