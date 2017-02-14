namespace NsqSharp.Bus
{
    /// <summary>
    /// Implement to register a class as a message finalizer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFinalizeMessages<T>
    {
        /// <summary>
        /// Handles successful message execution.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        void MessageSucceeded(T message);
        /// <summary>
        /// Handles message processing failure on final retry.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        void MessageFailed(T message);
    }
}
