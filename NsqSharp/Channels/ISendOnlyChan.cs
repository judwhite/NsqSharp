namespace NsqSharp.Channels
{
    /// <summary>
    /// ISendOnlyChan interface.
    /// </summary>
    /// <typeparam name="T">The type of data sent on the channel</typeparam>
    public interface ISendOnlyChan<T> : ISendOnlyChan
    {
    }

    /// <summary>
    /// ISendOnlyChan interface.
    /// </summary>
    public interface ISendOnlyChan : IChan
    {
        /// <summary>
        /// Sends a message to the channel. Blocks until the message is received.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void Send(object message);

        /// <summary>
        /// Gets a value indicating if the channel is ready to receive and waiting for a sender.
        /// </summary>
        bool IsReadyToReceive { get; }

        /// <summary>
        /// Tries to lock the send method to the current thread.
        /// </summary>
        /// <returns><c>true</c> if the lock was successful; otherwise, <c>false</c>.</returns>
        bool TryLockSend();

        /// <summary>
        /// Unlocks the send method.
        /// </summary>
        void UnlockSend();
    }
}
