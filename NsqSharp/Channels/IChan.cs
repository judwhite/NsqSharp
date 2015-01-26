using System.Threading;

namespace NsqSharp.Channels
{
    internal interface IChan
    {
        void Send(object message);
        object Receive();

        bool IsReadyToReceive { get; }
        bool IsReadyToSend { get; }
        bool IsClosed { get; }

        void AddListener(AutoResetEvent func);
        void RemoveListener(AutoResetEvent autoResetEvent);

        bool TryLockReceive();
        bool TryLockSend();
        void UnlockReceive();
        void UnlockSend();
    }
}
