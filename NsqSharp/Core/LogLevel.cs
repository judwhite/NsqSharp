namespace NsqSharp.Core
{
    // https://github.com/nsqio/go-nsq/blob/master/delegates.go

    /// <summary>
    /// Core.LogLevel specifies the severity of a given log message
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Debug</summary>
        Debug = 0,
        /// <summary>Info</summary>
        Info = 1,
        /// <summary>Warning</summary>
        Warning = 2,
        /// <summary>Error</summary>
        Error = 3,
        /// <summary>Critical</summary>
        Critical = 4,
    }
}
