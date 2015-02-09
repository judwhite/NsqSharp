using System.Threading;

namespace NsqSharp.Channels
{
    /// <summary>
    /// IChan interface.
    /// </summary>
    public interface IChan
    {
        /// <summary>Gets a value indicating whether the channel is closed.</summary>
        bool IsClosed { get; }

        /// <summary>Add a listener which will be notified when a channel is ready to send.</summary>
        void AddListenForSend(AutoResetEvent func);

        /// <summary>Add a listener which will be notified when a channel is ready to receive.</summary>
        void AddListenForReceive(AutoResetEvent func);

        /// <summary>Remove a listener for send.</summary>
        void RemoveListenForSend(AutoResetEvent autoResetEvent);

        /// <summary>Remove a listener for send.</summary>
        void RemoveListenForReceive(AutoResetEvent autoResetEvent);
    }
}
