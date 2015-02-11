using System;
using System.Diagnostics;
using System.IO;
using NsqSharp.Go;

namespace NsqSharp.Utils
{
    /// <summary>
    /// Console logger
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly TextWriter _textWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        public ConsoleLogger()
        {
            _textWriter = new StreamWriter(Console.OpenStandardOutput());
        }

        /// Output writes the output for a logging event. The string s contains
        /// the text to print after the prefix specified by the flags of the
        /// Logger. A newline is appended if the last character of s is not
        /// already a newline. Calldepth is used to recover the PC and is
        /// provided for generality, although at the moment on all pre-defined
        /// paths it will be 2.
        public void Output(int calldepth, string s)
        {
            _textWriter.WriteLine(s);
            _textWriter.Flush();
            Debug.WriteLine(s);
        }
    }
}
