namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Implement this interface to modify a message before it is sent.
    /// </summary>
    public interface IMessageMutator
    {
        /// <summary>
        /// Gets a mutated message before it is sent.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus sending this message.</param>
        /// <param name="sentMessage">The message about to be sent.</param>
        /// <returns>The mutated message.</returns>
        T GetMutatedMessage<T>(IBus bus, T sentMessage);
    }
}
