using System;
using System.Collections;
using System.Collections.Generic;

namespace NsqSharp.Bus.Configuration.Providers
{
    /// <summary>
    /// Message finalizer metatada
    /// </summary>
    public class MessageFinalizerInfo
    {
        /// <summary>
        /// A message handler type
        /// </summary>
        public Type MessageHandlerType { get; set; }
        /// <summary>
        /// A message finalizer type
        /// </summary>
        public Type MessageFinalizerType { get; set; }
    }

    /// <summary>
    /// Implement <see cref="IMessageFinalizerProvider"/> to specify messages need to be finalized.
    /// See <see cref="IHandleMessages&lt;T&gt;"/>, <see cref="BusConfiguration"/>,
    /// and <see cref="IMessageTypeToTopicProvider"/>.
    /// </summary>
    public interface IMessageFinalizerProvider
    {
        /// <summary>
        /// Gets the registered finalizer types implementing <see cref="IFinalizeMessages&lt;T&gt;"/>.
        /// </summary>
        /// <returns>The registered finalizer types implementing <see cref="IFinalizeMessages&lt;T&gt;"/>.</returns>
        IEnumerable<MessageFinalizerInfo> GetMessageFinalizers();
    }
}