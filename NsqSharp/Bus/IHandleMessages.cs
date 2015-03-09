namespace NsqSharp.Bus
{
    /// <summary>
    /// Implement to register a class as a message handler.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public interface IHandleMessages<T>
    {
        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        void Handle(T message);
    }
}
