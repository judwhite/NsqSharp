using System;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/errors.go

    /// <summary>
    /// ErrNotConnected is returned when a publish command is made
    /// against a Producer that is not connected
    /// </summary>
    public class ErrNotConnected : Exception
    {
        /// <summary>Initializes a new instance of the ErrNotConnected class.</summary>
        public ErrNotConnected()
            : base("not connected")
        {
        }
    }

    /// <summary>
    /// ErrStopped is returned when a publish command is
    /// made against a Producer that has been stopped 
    /// </summary>
    public class ErrStopped : Exception
    {
        /// <summary>Initializes a new instance of the ErrStopped class.</summary>
        public ErrStopped()
            : base("stopped")
        {
        }
    }

    /// <summary>
    /// ErrAlreadyConnected is returned from ConnectToNSQD when already connected
    /// </summary>
    public class ErrAlreadyConnected : Exception
    {
        /// <summary>Initializes a new instance of the ErrAlreadyConnected class.</summary>
        public ErrAlreadyConnected()
            : base("already connected")
        {
        }
    }

    /// <summary>
    /// ErrOverMaxInFlight is returned from Consumer if over max-in-flight
    /// </summary>
    public class ErrOverMaxInFlight : Exception
    {
        /// <summary>Initializes a new instance of the ErrOverMaxInFlight class.</summary>
        public ErrOverMaxInFlight()
            : base("over configure max-inflight")
        {
        }
    }

    /// <summary>
    /// ErrIdentify is returned from Conn as part of the IDENTIFY handshake
    /// </summary>
    public class ErrIdentify : Exception
    {
        /// <summary>Initializes a new instance of the ErrIdentify class.</summary>
        public ErrIdentify(string reason)
            : base(string.Format("failed to IDENTIFY - {0}", reason))
        {
            Reason = reason;
        }

        /// <summary>Initializes a new instance of the ErrIdentify class.</summary>
        public ErrIdentify(string reason, Exception innerException)
            : base(string.Format("failed to IDENTIFY - {0}", reason), innerException)
        {
        }

        /// <summary>Reason</summary>
        public string Reason { get; private set; }
    }

    /// <summary>
    /// ErrProtocol is returned from Producer when encountering
    /// an NSQ protocol level error
    /// </summary>
    public class ErrProtocol : Exception
    {
        /// <summary>Initializes a new instance of the ErrProtocol class.</summary>
        public ErrProtocol(string reason)
            : base(reason)
        {
            Reason = reason;
        }

        /// <summary>Reason</summary>
        public string Reason { get; private set; }
    }
}
