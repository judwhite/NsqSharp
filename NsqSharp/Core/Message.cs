using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NsqSharp.Utils;
using System.Text;

namespace NsqSharp.Core
{
    // https://github.com/bitly/go-nsq/blob/master/message.go

    /// <summary>
    /// Message is the fundamental data type containing
    /// the id, body, and metadata
    /// </summary>
    [DebuggerDisplay("Id={Id}, Attempts={Attempts}, TS={Timestamp}, NSQD={NsqdAddress}")]
    public class Message
    {
        /// <summary>The number of bytes for a Message.ID</summary>
        internal const int MsgIdLength = 16;

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1);

        /// <summary>ID</summary>
        internal byte[] ID { get; set; }
        /// <summary>Body</summary>
        public byte[] Body { get; internal set; }
        /// <summary>Timestamp</summary>
        public DateTime Timestamp { get; internal set; }
        /// <summary>Attempts</summary>
        public int Attempts { get; internal set; }
        /// <summary>Max Attempts</summary>
        public int MaxAttempts { get; internal set; }

        /// <summary>NsqdAddress</summary>
        public string NsqdAddress { get; internal set; }

        /// <summary>Delegate</summary>
        internal IMessageDelegate Delegate { get; set; }

        private int _autoResponseDisabled;
        private int _responded;
        private string _idHexString;

        /// <summary>
        /// Creates a Message, initializes some metadata,
        /// and returns a pointer
        /// </summary>
        /// <param name="id">The message ID</param>
        /// <param name="body">The message body</param>
        public Message(byte[] id, byte[] body)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (id.Length != MsgIdLength)
                throw new ArgumentOutOfRangeException("id", id.Length, string.Format("id length must be {0} bytes", MsgIdLength));
            if (body == null)
                throw new ArgumentNullException("body");

            ID = id;
            Body = body;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// DisableAutoResponse disables the automatic response that
        /// would normally be sent when a handler.HandleMessage
        /// returns (FIN/REQ based on the error value returned).
        ///
        /// This is useful if you want to batch, buffer, or asynchronously
        /// respond to messages.
        /// </summary>
        public void DisableAutoResponse()
        {
            // the CLR/CLI guarantees atomic reads/writes to ints
            // see: http://blogs.msdn.com/b/ericlippert/archive/2011/05/31/atomicity-volatility-and-immutability-are-different-part-two.aspx
            _autoResponseDisabled = 1;
        }

        /// <summary>
        /// IsAutoResponseDisabled indicates whether or not this message
        /// will be responded to automatically
        /// </summary>
        public bool IsAutoResponseDisabled()
        {
            return (_autoResponseDisabled == 1);
        }

        /// <summary>
        /// HasResponded indicates whether or not this message has been responded to
        /// </summary>
        public bool HasResponded()
        {
            return (_responded == 1);
        }

        /// <summary>
        /// Finish sends a FIN command to the nsqd which
        /// sent this message
        /// </summary>
        public void Finish()
        {
            if (Interlocked.CompareExchange(ref _responded, value: 1, comparand: 0) == 1)
            {
                return;
            }
            Delegate.OnFinish(this);
        }

        /// <summary>
        /// Touch sends a TOUCH command to the nsqd which
        /// sent this message
        /// </summary>
        public void Touch()
        {
            if (HasResponded())
            {
                return;
            }
            Delegate.OnTouch(this);
        }

        /// <summary>
        /// Requeue sends a REQ command to the nsqd which
        /// sent this message, using the supplied delay.
        ///
        /// A delay of <c>null</c> will automatically calculate
        /// based on the number of attempts and the
        /// configured default_requeue_delay
        /// </summary>
        public void Requeue(TimeSpan? delay = null)
        {
            doRequeue(delay, backoff: true);
        }

        /// <summary>
        /// RequeueWithoutBackoff sends a REQ command to the nsqd which
        /// sent this message, using the supplied delay.
        ///
        /// Notably, using this method to respond does not trigger a backoff
        /// event on the configured Delegate.
        /// </summary>
        public void RequeueWithoutBackoff(TimeSpan? delay)
        {
            doRequeue(delay, backoff: false);
        }

        private void doRequeue(TimeSpan? delay, bool backoff)
        {
            if (Interlocked.CompareExchange(ref _responded, value: 1, comparand: 0) == 1)
            {
                return;
            }
            Delegate.OnRequeue(this, delay, backoff);
        }

        /// <summary>
        /// WriteTo implements the WriterTo interface and serializes
        /// the message into the supplied producer.
        ///
        /// It is suggested that the target Writer is buffered to
        /// avoid performing many system calls.
        /// </summary>
        public Int64 WriteTo(Stream w)
        {
            if (w == null)
                throw new ArgumentNullException("w");

            Int64 total;

            using (var writer = new BinaryWriter(w))
            {
                ulong ns = (ulong)(Timestamp - _epoch).Ticks * 100;
                Binary.BigEndian.PutUint64(writer, ns);
                Binary.BigEndian.PutUint16(writer, (ushort)Attempts);
                total = 10;

                writer.Write(ID);
                total += MsgIdLength;

                writer.Write(Body);
                total += Body.Length;
            }

            return total;
        }

        /// <summary>
        /// DecodeMessage deseralizes data (as []byte) and creates a new Message
        /// </summary>
        public static Message DecodeMessage(byte[] b)
        {
            if (b == null)
                throw new ArgumentNullException("b");

            using (var memoryStream = new MemoryStream(b))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                ulong timestamp = Binary.BigEndian.UInt64(binaryReader);
                ushort attempts = Binary.BigEndian.UInt16(binaryReader);

                var timeOffset = new TimeSpan((long)(timestamp / 100));

                byte[] id = binaryReader.ReadBytes(MsgIdLength);

                byte[] body = binaryReader.ReadBytes(b.Length - MsgIdLength - 10);

                return new Message(id, body) { Timestamp = _epoch + timeOffset, Attempts = attempts };
            }
        }

        /// <summary>
        /// The 16-byte message ID as a hex string.
        /// </summary>
        public string Id
        {
            get
            {
                if (_idHexString == null)
                {
                    _idHexString = Encoding.UTF8.GetString(ID);
                }

                return _idHexString;
            }
        }
    }
}
