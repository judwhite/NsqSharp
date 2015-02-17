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
        /// Sends a message on the bus.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="message">The message.</param>
        void Send<T>(T message);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void Send<T>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        void Send<T>(Action<T> messageConstructor);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="nsqdTcpAddresses"></param>
        void Send<T>(T message, params string[] nsqdTcpAddresses);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nsqdTcpAddresses"></param>
        void Send<T>(params string[] nsqdTcpAddresses);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        /// <param name="nsqdTcpAddresses"></param>
        void Send<T>(Action<T> messageConstructor, params string[] nsqdTcpAddresses);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="topic"></param>
        /// <param name="nsqdTcpAddresses"></param>
        void Send<T>(T message, string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="nsqdTcpAddresses"></param>
        void Send<T>(string topic, params string[] nsqdTcpAddresses);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageConstructor"></param>
        /// <param name="topic"></param>
        /// <param name="nsqdTcpAddresses"></param>
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
