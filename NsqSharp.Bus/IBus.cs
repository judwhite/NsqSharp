using System;
using System.Collections.Generic;

namespace NsqSharp.Bus
{
    /// <summary>
    /// IBus interface.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        void Send<T>(T message);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type (can be an interface).</typeparam>
        void Send<T>();

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using
        /// <paramref name="messageConstructor"/> to populate the message.
        /// </summary>
        /// <typeparam name="T">The message type (can be an interface).</typeparam>
        /// <param name="messageConstructor">The method used to populate the object.</param>
        void Send<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic
        /// to the local process.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        void SendLocal<T>(T message);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic
        /// to the local process.
        /// </summary>
        /// <typeparam name="T">The message type (can be an interface).</typeparam>
        void SendLocal<T>();

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using
        /// <paramref name="messageConstructor"/> to populate the message, to the local process.
        /// </summary>
        /// <typeparam name="T">The message type (can be an interface).</typeparam>
        /// <param name="messageConstructor">The method used to populate the object.</param>
        void SendLocal<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends messages of type <typeparamref name="T"/> on the configured topic.
        /// More efficient than calling <see cref="Send&lt;T&gt;(T)"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messages">The messages.</param>
        void SendMulti<T>(IEnumerable<T> messages);

        /*/// <summary>
        /// Defer send a message for <paramref name="duration"/>.
        /// </summary>
        void Defer<T>(TimeSpan duration, T message);

        /// <summary>Defer send a message until <paramref name="processAt"/>.</summary>
        void Defer<T>(DateTime processAt, T message);*/

        /// <summary>Gets the current NSQ message being processed.</summary>
        Message CurrentMessage { get; }
    }
}
