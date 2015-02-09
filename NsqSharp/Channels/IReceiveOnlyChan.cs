namespace NsqSharp.Channels
{
    /// <summary>
    /// IReceiveOnlyChan interface.
    /// </summary>
    /// <typeparam name="T">The type of data received on the channel.</typeparam>
    public interface IReceiveOnlyChan<T> : IReceiveOnlyChan
    {
    }

    /// <summary>
    /// IReceiveOnlyChan interface.
    /// </summary>
    public interface IReceiveOnlyChan : IChan
    {
        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready or channel is closed.
        /// </summary>
        /// <returns>The message received.</returns>
        object Receive();

        /// <summary>
        /// Receives a message from the channel. Blocks until a message is ready or channel is closed.
        /// </summary>
        /// <returns>The message received.</returns>
        object ReceiveOk(out bool ok);

        /// <summary>
        /// Gets a value indicating if the channel is ready to send and waiting for a receiver.
        /// </summary>
        bool IsReadyToSend { get; }
        
        /// <summary>
        /// Tries to lock the receive method to the current thread.
        /// </summary>
        /// <returns><c>true</c> if the lock was successful; otherwise, <c>false</c>.</returns>
        bool TryLockReceive();
        
        /// <summary>
        /// Unlocks the receive method.
        /// </summary>
        void UnlockReceive();
    }
}
