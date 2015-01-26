namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/states.go

    /// <summary>
    /// States
    /// </summary>
    public enum State
    {
        /// <summary>Init</summary>
        Init = 0,

        /// <summary>Disconnected</summary>
        Disconnected = 1,

        /// <summary>Connected</summary>
        Connected = 2,

        /// <summary>Subscribed</summary>
        Subscribed = 3,

        /// <summary>
        /// Closing means CLOSE has started...
        /// (responses are ok, but no new messages will be sent)
        /// </summary>
        Closing = 4,
    }
}
