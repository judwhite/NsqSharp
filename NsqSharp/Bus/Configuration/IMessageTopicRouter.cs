using NsqSharp.Bus.Configuration.Providers;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Implement this interface to specify custom message-to-topic routing logic based on a message object about to be sent.
    /// </summary>
    public interface IMessageTopicRouter
    {
        /// <summary>
        /// Gets the topic a message should be sent on.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus sending this message.</param>
        /// <param name="originalTopic">The original topic name as provided by the implementation
        /// of <see cref="IMessageTypeToTopicProvider"/> passed to this bus.</param>
        /// <param name="sentMessage">The message about to be sent.</param>
        /// <returns>The topic to send this message on.</returns>
        string GetMessageTopic<T>(IBus bus, string originalTopic, T sentMessage);
    }
}
