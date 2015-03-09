using NsqSharp.Core;

namespace NsqSharp.Bus.Logging
{
    /// <summary>
    /// The category of mesage failure.
    /// </summary>
    public enum FailedMessageReason
    {
        /// <summary>
        /// The dependency injection container failed to build the object or the handler constructor threw an exception.
        /// </summary>
        HandlerConstructor,
        /// <summary>
        /// The <see cref="Message.Body"/> failed to deserialize.
        /// </summary>
        MessageDeserialization,
        /// <summary>
        /// The <see cref="IHandleMessages&lt;T&gt;.Handle"/> implementation threw an exception.
        /// </summary>
        HandlerException,
        /// <summary>
        /// The maximum number of attempts was exceeded.
        /// </summary>
        MaxAttemptsExceeded
    }
}
