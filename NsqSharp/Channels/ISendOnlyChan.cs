using System;

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
        /// Tries to lock the send method to the current thread.
        /// </summary>
        /// <returns><c>true</c> if the lock was successful; otherwise, <c>false</c>.</returns>
        bool TryLockSend();

        /// <summary>
        /// Unlocks the send method.
        /// </summary>
        void UnlockSend();

        /// <summary>
        /// Tries to send a message to the channel. Blocks until the message is received 
        /// or the <paramref name="timeout"/>  expires.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The timout period.</param>
        /// <returns><c>true</c> if the message was sent; otherwise, <c>false</c>.</returns>
        bool TrySend(object message, TimeSpan timeout);
    }
}
