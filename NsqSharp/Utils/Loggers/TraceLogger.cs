using System.Diagnostics;
using NsqSharp.Core;

namespace NsqSharp.Utils.Loggers
{
    /// <summary>
    /// Trace logger
    /// </summary>
    public class TraceLogger : ILogger
    {
        private readonly TraceSource _traceSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceLogger"/> class.
        /// </summary>
        public TraceLogger()
        {
            _traceSource = new TraceSource("NsqSharp");
        }

        /// Output writes the output for a logging event. The string s contains
        /// the text to print after the prefix specified by the flags of the
        /// Logger. A newline is appended if the last character of s is not
        /// already a newline.
        public void Output(LogLevel logLevel, string message)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    _traceSource.TraceEvent(TraceEventType.Verbose, 0, message);
                    break;
                case LogLevel.Info:
                    _traceSource.TraceEvent(TraceEventType.Information, 0, message);
                    break;
                case LogLevel.Warning:
                    _traceSource.TraceEvent(TraceEventType.Warning, 0, message);
                    break;
                case LogLevel.Error:
                    _traceSource.TraceEvent(TraceEventType.Error, 0, message);
                    break;
            }
        }
    }
}
