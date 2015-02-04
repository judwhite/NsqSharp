using NsqSharp.Go;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/conn.go

    /// <summary>
    /// IdentifyResponse represents the metadata
    /// returned from an IDENTIFY command to nsqd
    /// </summary>
    public class IdentifyResponse
    {
        // TODO
    }

    /// <summary>
    /// Conn represents a connection to nsqd
    ///
    /// Conn exposes a set of callbacks for the
    /// various events that occur on a connection
    /// </summary>
    public class Conn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Conn"/> class.
        /// </summary>
        public Conn(string addr, Config config, IConnDelegate connDelegate)
        {
            // TODO
        }

        // TODO

        /// <summary>
        /// SetLogger assigns the logger to use as well as a level.
        ///
        /// The format parameter is expected to be a printf compatible string with
        /// a single {0} argument.  This is useful if you want to provide additional
        /// context to the log messages that the connection will print, the default
        /// is '({0})'.
        ///
        /// The logger parameter is an interface that requires the following
        /// method to be implemented (such as the the stdlib log.Logger):
        ///
        ///    Output(calldepth int, s string)
        ///
        /// </summary>
        public void SetLogger(ILogger l, LogLevel lvl, string format)
        {
            // TODO
        }

        /// <summary>
        /// Connect dials and bootstraps the nsqd connection
        /// (including IDENTIFY) and returns the IdentifyResponse
        /// </summary>
        public IdentifyResponse Connect()
        {
            // TODO
            return new IdentifyResponse();
        }

        /// <summary>
        /// Close idempotently initiates connection close
        /// </summary>
        public void Close()
        {
            // TODO
        }

        // TODO

        /// <summary>
        /// WriteCommand is a goroutine safe method to write a Command
        /// to this connection, and flush.
        /// </summary>
        public void WriteCommand(Command cmd)
        {
            // TODO
        }

        // TODO
    }
}
