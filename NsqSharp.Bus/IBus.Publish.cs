using System;

namespace NsqSharp.Bus
{
    /// <summary>
    /// IBus interface.
    /// </summary>
    public partial interface IBus
    {
        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        void Publish<T>(T message);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        void Publish<T>();

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using
        /// <paramref name="messageConstructor"/> to populate the message.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageConstructor">The method used to populate the object.</param>
        void Publish<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Publish<T>(T message, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Publish<T>(params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using 
        /// <paramref name="messageConstructor"/> to populate the message, to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageConstructor">The message constructor.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Publish<T>(Action<T> messageConstructor, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Publish<T>(T message, string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Publish<T>(string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>, using
        /// <paramref name="messageConstructor"/> to populate the message, to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messageConstructor">The message constructor.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        void Publish<T>(Action<T> messageConstructor, string topic, params string[] nsqdTcpAddresses);
    }
}
