using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using NsqSharp.Core;
using NsqSharp.Utils;

namespace NsqSharp
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
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="id">The message ID.</param>
        /// <param name="body">The message body.</param>
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
        /// <para>Disables the automatic response that
        /// would normally be sent when <see cref="IHandler.HandleMessage"/>
        /// returns or throw.</para>
        ///
        /// <para>This is useful if you want to batch, buffer, or asynchronously
        /// respond to messages.</para>
        /// </summary>
        public void DisableAutoResponse()
        {
            // the CLR/CLI guarantees atomic reads/writes to ints
            // see: http://blogs.msdn.com/b/ericlippert/archive/2011/05/31/atomicity-volatility-and-immutability-are-different-part-two.aspx
            _autoResponseDisabled = 1;
        }

        /// <summary>
        /// Indicates whether or not this message
        /// will be responded to automatically.
        /// </summary>
        public bool IsAutoResponseDisabled
        {
            get { return (_autoResponseDisabled == 1); }
        }

        /// <summary>
        /// Indicates whether or not this message has been responded to.
        /// </summary>
        public bool HasResponded
        {
            get { return (_responded == 1); }
        }

        /// <summary>
        /// Finish sends a FIN command to the nsqd which
        /// sent this message.
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
        /// Touch sends a TOUCH command to the nsqd which sent this message, resetting the default timeout.
        /// The server-default timeout is 60s; see <see cref="Config.MessageTimeout"/>.
        /// </summary>
        public void Touch()
        {
            if (HasResponded)
            {
                return;
            }
            Delegate.OnTouch(this);
        }

        /// <summary>
        /// <para>Sends a REQ command to the nsqd which
        /// sent this message, using the supplied delay.</para>
        ///
        /// <para>A delay of <c>null</c> will automatically calculate
        /// based on the number of attempts and the
        /// configured <see cref="Config.DefaultRequeueDelay"/>.</para>
        /// 
        /// <para>Using this method to respond triggers a backoff event.</para>
        /// </summary>
        public void Requeue(TimeSpan? delay = null)
        {
            doRequeue(delay, backoff: true);
        }

        /// <summary>
        /// <para>Sends a REQ command to the nsqd which
        /// sent this message, using the supplied delay.</para>
        ///
        /// <para>Using this method to respond does not trigger a backoff event.</para>
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

            var requeueTimeSpan = Delegate.OnRequeue(this, delay, backoff);
            RequeuedUntil = DateTime.UtcNow + requeueTimeSpan;
            Backoff = backoff;
        }

        /// <summary>
        /// Gets a value indicating whether this message triggered a backoff event.
        /// </summary>
        public bool Backoff { get; private set; }

        /// <summary>
        /// Gets the minimum date/time the message will be requeued until.
        /// </summary>
        public DateTime? RequeuedUntil { get; private set; }

        /// <summary>
        /// <para>WriteTo implements the WriterTo interface and serializes
        /// the message into the supplied producer.</para>
        ///
        /// <para>It is suggested that the target Writer is buffered to
        /// avoid performing many system calls.</para>
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
        /// Deseralizes <paramref name="data"/> and creates a new <see cref="Message"/>.
        /// </summary>
        /// <param name="data">The fully encoded message.</param>
        /// <returns>The decoded message.</returns>
        public static Message DecodeMessage(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("b");

            using (var memoryStream = new MemoryStream(data))
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                ulong timestamp = Binary.BigEndian.UInt64(binaryReader);
                ushort attempts = Binary.BigEndian.UInt16(binaryReader);

                var timeOffset = new TimeSpan((long)(timestamp / 100));

                byte[] id = binaryReader.ReadBytes(MsgIdLength);

                byte[] body = binaryReader.ReadBytes(data.Length - MsgIdLength - 10);

                return new Message(id, body) { Timestamp = _epoch + timeOffset, Attempts = attempts };
            }
        }

        /// <summary>
        /// The message ID as a hex string.
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
