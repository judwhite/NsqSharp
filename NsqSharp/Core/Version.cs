namespace NsqSharp.Core
{
    // https://github.com/bitly/go-nsq/blob/master/version.go

    /// <summary>
    /// NSQ Client Information
    /// </summary>
    public static class ClientInfo
    {
        static ClientInfo()
        {
            var version = typeof (ClientInfo).Assembly.GetName().Version;
            Version = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        /// <summary>Version</summary>
        public static string Version { get; private set; }

        /// <summary>Client name</summary>
        public const string ClientName = "NsqSharp";
    }
}
