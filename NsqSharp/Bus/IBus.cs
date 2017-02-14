﻿using System;
using System.Collections.Generic;

namespace NsqSharp.Bus
{
    /// <summary>
    /// IBus interface.
    /// </summary>
    public interface IBus
    {
        /// <summary>
        /// Sends a <paramref name="message"/> of type <paramref name="messsageType"/> on the configured topic.
        /// 
        /// </summary>
        /// <param name="messsageType">The message type.</param>
        /// <param name="message">The message.</param>
        void Send(Type messsageType, object message);

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
        /// Sends messages of type <typeparamref name="T"/> on the configured topic.
        /// More efficient than calling <see cref="Send&lt;T&gt;(T)"/>.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="messages">The messages.</param>
        void SendMulti<T>(IEnumerable<T> messages);

        /// <summary>Gets the current NSQ message being processed. Returns <c>null</c> if the current thread isn't
        /// a thread started to handle a message.</summary>
        IMessage CurrentThreadMessage { get; }

        /// <summary>Gets <see cref="ICurrentMessageInformation"/> about the current message being processed. Returns
        /// <c>null</c> if the current thread isn't a thread started to handle a message.</summary>
        ICurrentMessageInformation GetCurrentThreadMessageInformation();
    }
}
