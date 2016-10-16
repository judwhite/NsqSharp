using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace NsqSharp.Core
{
    // https://github.com/nsqio/go-nsq/blob/master/errors.go

    /// <summary>
    /// ErrNotConnected is returned when a publish command is made
    /// against a Producer that is not connected
    /// </summary>
    [Serializable]
    public class ErrNotConnected : Exception
    {
        /// <summary>Initializes a new instance of the ErrNotConnected class.</summary>
        public ErrNotConnected()
            : base("not connected")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrNotConnected"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ErrNotConnected(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// ErrStopped is returned when a publish command is
    /// made against a Producer that has been stopped 
    /// </summary>
    [Serializable]
    public class ErrStopped : Exception
    {
        /// <summary>Initializes a new instance of the ErrStopped class.</summary>
        public ErrStopped()
            : base("stopped")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrStopped"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ErrStopped(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// ErrClosing is returned when a connection is closing
    /// </summary>
    [Serializable]
    public class ErrClosing : Exception
    {
        /// <summary>Initializes a new instance of the ErrClosing class.</summary>
        public ErrClosing()
            : base("closing")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrClosing"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ErrClosing(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// ErrOverMaxInFlight is returned from Consumer if over max-in-flight
    /// </summary>
    [Serializable]
    public class ErrOverMaxInFlight : Exception
    {
        /// <summary>Initializes a new instance of the ErrOverMaxInFlight class.</summary>
        public ErrOverMaxInFlight()
            : base("over configured max-inflight")
        {
            // TODO: go-nsq PR: fix typo "over configure"
            // https://github.com/nsqio/go-nsq/blob/08a850b52c79a9a1b6e457233bd11bf7ba713178/errors.go#L23
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrOverMaxInFlight"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ErrOverMaxInFlight(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// ErrIdentify is returned from Conn as part of the IDENTIFY handshake
    /// </summary>
    [Serializable]
    public class ErrIdentify : Exception
    {
        /// <summary>Initializes a new instance of the ErrIdentify class.</summary>
        public ErrIdentify(string reason)
            : this(reason, null)
        {
        }

        /// <summary>Initializes a new instance of the ErrIdentify class.</summary>
        public ErrIdentify(string reason, Exception innerException)
            : base(string.Format("failed to IDENTIFY - {0}", reason), innerException)
        {
            Reason = reason;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrIdentify"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ErrIdentify(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Reason = info.GetString("Reason");
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception 
        /// being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or
        /// destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is a null reference.</exception>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            info.AddValue("Reason", Reason);

            base.GetObjectData(info, context);
        }

        /// <summary>
        /// Gets or sets the reason
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// ErrProtocol is returned from Producer when encountering
    /// an NSQ protocol level error
    /// </summary>
    [Serializable]
    public class ErrProtocol : Exception
    {
        /// <summary>Initializes a new instance of the ErrProtocol class.</summary>
        public ErrProtocol(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrProtocol"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about
        /// the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about
        /// the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/>
        /// is zero (0).</exception>
        protected ErrProtocol(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
