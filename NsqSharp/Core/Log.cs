namespace NsqSharp.Core
{
    // https://github.com/nsqio/go-nsq/blob/master/delegates.go

    /// <summary>
    /// Logging constants
    /// </summary>
    internal static class Log
    {
        /// <summary>LogLevelDebugPrefix</summary>
        public const string DebugPrefix = "DBG";
        /// <summary>LogLevelInfoPrefix</summary>
        public const string InfoPrefix = "INF";
        /// <summary>LogLevelWarningPrefix</summary>
        public const string WarningPrefix = "WRN";
        /// <summary>LogLevelErrorPrefix</summary>
        public const string ErrorPrefix = "ERR";
        /// <summary>LogLevelCriticalPrefix</summary>
        public const string CriticalPrefix = "FAT";

        /// <summary>LogPrefix Resolution</summary>
        internal static string Prefix(LogLevel lvl)
        {
            string prefix = string.Empty;

            switch (lvl)
            {
                case LogLevel.Debug:
                    prefix = DebugPrefix;
                    break;
                case LogLevel.Info:
                    prefix = InfoPrefix;
                    break;
                case LogLevel.Warning:
                    prefix = WarningPrefix;
                    break;
                case LogLevel.Error:
                    prefix = ErrorPrefix;
                    break;
                case LogLevel.Critical:
                    prefix = CriticalPrefix;
                    break;
            }

            return prefix;
        }
    }
}
