using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NsqSharp.Channels;
using NsqSharp.Go;

namespace NsqSharp
{
    /// <summary>
    /// Producer is a high-level type to publish to NSQ.
    ///
    /// A Producer instance is 1:1 with a destination `nsqd`
    /// and will lazily connect to that instance (and re-connect)
    /// when Publish commands are executed.
    /// </summary>
    public partial class Producer
    {
        //private long _id;
        //private string _addr;
        //private Conn _conn;
        //private Config _config;

        //private Logger _logger;
        /// <summary>Log level</summary>
        public LogLevel LogLevel { get; set; }

        //private Chan<byte[]> _responseChan;
        //private Chan<byte[]> _errorChan;
        //private Chan<int> _closeChan;

        //private Chan<ProducerTransaction> _transactionChan;
        //private Slice<ProducerTransaction> _transactions;
        //private int _state;

        //private int _concurrentProducers;
        //private int _stopFlag;
        //private Chan<int> _exitChan;
        //private WaitGroup _wg;
        //private readonly object _guard = new object();
    }

    /// <summary>
    /// ProducerTransaction is returned by the async publish methods
    /// to retrieve metadata about the command after the
    /// response is received.
    /// </summary>
    public class ProducerTransaction
    {
        private Command cmd { get; set; }
        private Chan<ProducerTransaction> doneChan { get; set; }

        /// <summary>
        /// the error (or nil) of the publish command
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// the slice of variadic arguments passed to PublishAsync or MultiPublishAsync
        /// </summary>
        public object[] Args { get; set; }

        private void finish()
        {
            if (doneChan != null)
            {
                doneChan.Send(this);
            }
        }
    }

    public partial class Producer
    {
        /// <summary>
        /// Initializes a new instance of the Producer class.
        /// </summary>
        /// <param name="addr">The address.</param>
        /// <param name="config">The config. After Config is passed into NewProducer the values are no longer mutable (they are copied).</param>
        public Producer(string addr, Config config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            config.Validate();

            //_addr = addr;
            //_config = config.Clone();

            //_logger = new Logger(...); // TODO
            LogLevel = LogLevel.Info;

            /*_transactionChan = new Chan<ProducerTransaction>();
            _exitChan = new Chan<int>();
            _responseChan = new Chan<byte[]>();
            _errorChan = new Chan<byte[]>();*/
        }

        // TODO
    }
}
