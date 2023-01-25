namespace NsqSharp.Core
{
    // https://github.com/nsqio/go-nsq/blob/master/delegates.go

    /// <summary>
    /// Logging constants
    /// </summary>
    internal static class Log
    {
        /// <summary>Core.LogLevelDebugPrefix</summary>
        public const string DebugPrefix = "DBG";
        /// <summary>Core.LogLevelInfoPrefix</summary>
        public const string InfoPrefix = "INF";
        /// <summary>Core.LogLevelWarningPrefix</summary>
        public const string WarningPrefix = "WRN";
        /// <summary>Core.LogLevelErrorPrefix</summary>
        public const string ErrorPrefix = "ERR";
        /// <summary>Core.LogLevelCriticalPrefix</summary>
        public const string CriticalPrefix = "FAT";

        /// <summary>LogPrefix Resolution</summary>
        internal static string Prefix(Core.LogLevel lvl)
        {
            string prefix = string.Empty;

            switch (lvl)
            {
                case Core.LogLevel.Debug:
                    prefix = DebugPrefix;
                    break;
                case Core.LogLevel.Info:
                    prefix = InfoPrefix;
                    break;
                case Core.LogLevel.Warning:
                    prefix = WarningPrefix;
                    break;
                case Core.LogLevel.Error:
                    prefix = ErrorPrefix;
                    break;
                case Core.LogLevel.Critical:
                    prefix = CriticalPrefix;
                    break;
            }

            return prefix;
        }
    }
}
