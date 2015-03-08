namespace NsqSharp.Bus.Logging
{
    /// <summary>
    /// The queue action taken for the failed message.
    /// </summary>
    public enum FailedMessageQueueAction
    {
        /// <summary>
        /// Message was requeued.
        /// </summary>
        Requeue,
        /// <summary>
        /// Message was failed permanently.
        /// </summary>
        Finish
    }
}
