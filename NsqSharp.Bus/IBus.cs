using System;
using System.Runtime.Remoting.Messaging;

namespace NsqSharp.Bus
{
    /// <summary>
    /// IBus interface.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the bus.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        void Send<T>(T message);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        void Send<T>();

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using
        /// <paramref name="messageConstructor"/> to populate the message.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageConstructor">The method used to populate the object.</param>
        void Send<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Send<T>(T message, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Send<T>(params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using 
        /// <paramref name="messageConstructor"/> to populate the message, to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageConstructor">The message constructor.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Send<T>(Action<T> messageConstructor, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Send<T>(T message, string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Send<T>(string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>, using
        /// <paramref name="messageConstructor"/> to populate the message, to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageConstructor">The message constructor.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Send<T>(Action<T> messageConstructor, string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Defer the current message for <paramref name="duration"/>.
        /// </summary>
        void Defer(TimeSpan duration);

        /// <summary>Defer the current message until <paramref name="processAt"/>.</summary>
        void Defer(DateTime processAt);

        /// <summary>Gets the current NSQ message being processed.</summary>
        IMessage CurrentMessage { get; }
    }
}
