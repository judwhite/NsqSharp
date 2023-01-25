﻿using System.Diagnostics;
using NsqSharp.Core;

namespace NsqSharp.Utils.Loggers
{
    /// <summary>
    /// Trace logger
    /// </summary>
    public class TraceLogger : Core.ILogger
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
        public void Output(Core.LogLevel logLevel, string message)
        {
            switch (logLevel)
            {
                case Core.LogLevel.Debug:
                    _traceSource.TraceEvent(TraceEventType.Verbose, 0, message);
                    break;
                case Core.LogLevel.Info:
                    _traceSource.TraceEvent(TraceEventType.Information, 0, message);
                    break;
                case Core.LogLevel.Warning:
                    _traceSource.TraceEvent(TraceEventType.Warning, 0, message);
                    break;
                case Core.LogLevel.Error:
                    _traceSource.TraceEvent(TraceEventType.Error, 0, message);
                    break;
                case Core.LogLevel.Critical:
                    _traceSource.TraceEvent(TraceEventType.Critical, 0, message);
                    break;
            }
        }

        /// <summary>
        /// Flushes the output stream.
        /// </summary>
        public void Flush()
        {
            _traceSource.Flush();
        }
    }
}
