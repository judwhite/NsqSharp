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

        /// <summary>Add a listener which will be notified when a channel is ready to either send or receive.</summary>
        void AddListener(AutoResetEvent func);

        /// <summary>Remove a listener.</summary>
        void RemoveListener(AutoResetEvent autoResetEvent);
    }
}
