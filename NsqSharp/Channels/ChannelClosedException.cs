using System;

namespace NsqSharp.Channels
{
    /// <summary>
    /// Occurs when attempt to send or receive from a closed channel.
    /// </summary>
    public class ChannelClosedException : Exception
    {
    }
}
