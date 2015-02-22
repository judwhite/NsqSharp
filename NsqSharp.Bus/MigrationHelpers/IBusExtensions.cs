using System;

namespace NsqSharp.Bus
{
    /// <summary>
    /// IBus extension methods.
    /// </summary>
    public static class IBusExtensions
    {
        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="message">The message.</param>
        public static void Publish<T>(this IBus bus, T message)
        {
            bus.Send(message);
        }

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        public static void Publish<T>(this IBus bus)
        {
            bus.Send<T>();
        }

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using
        /// <paramref name="messageConstructor"/> to populate the message.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="messageConstructor">The method used to populate the object.</param>
        public static void Publish<T>(this IBus bus, Action<T> messageConstructor)
        {
            bus.Send(messageConstructor);
        }

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="message">The message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        public static void Publish<T>(this IBus bus, T message, params string[] nsqdTcpAddresses)
        {
            bus.Send(message, nsqdTcpAddresses);
        }

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the configured topic
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        public static void Publish<T>(this IBus bus, params string[] nsqdTcpAddresses)
        {
            bus.Send<T>(nsqdTcpAddresses);
        }

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the configured topic, using 
        /// <paramref name="messageConstructor"/> to populate the message, to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="messageConstructor">The message constructor.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        public static void Publish<T>(this IBus bus, Action<T> messageConstructor, params string[] nsqdTcpAddresses)
        {
            bus.Send(messageConstructor, nsqdTcpAddresses);
        }

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="message">The message.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        public static void Publish<T>(this IBus bus, T message, string topic, params string[] nsqdTcpAddresses)
        {
            bus.Send(message, topic, nsqdTcpAddresses);
        }

        /// <summary>
        /// Sends an empty message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>
        /// to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        public static void Publish<T>(this IBus bus, string topic, params string[] nsqdTcpAddresses)
        {
            bus.Send<T>(topic, nsqdTcpAddresses);
        }

        /// <summary>
        /// Sends a message of type <typeparamref name="T"/> on the specified <paramref name="topic"/>, using
        /// <paramref name="messageConstructor"/> to populate the message, to the specified <paramref name="nsqdTcpAddresses"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="bus">The bus.</param>
        /// <param name="messageConstructor">The message constructor.</param>
        /// <param name="topic">The topic to receive this message.</param>
        /// <param name="nsqdTcpAddresses">The nsqd address(es) to receive the message.</param>
        public static void Publish<T>(this IBus bus, Action<T> messageConstructor, string topic, params string[] nsqdTcpAddresses)
        {
            bus.Send(messageConstructor, topic, nsqdTcpAddresses);
        }
    }
}
