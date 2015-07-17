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
    ///     Message is the fundamental data type containing the <see cref="Id"/>, <see cref="Body"/>, and metadata of a
    ///     message received from an nsqd instance.
    /// </summary>
    [DebuggerDisplay("Id={Id}, Attempts={Attempts}, TS={Timestamp}, NSQD={NsqdAddress}")]
    public sealed class Message : IMessage
    {
        /// <summary>The number of bytes for a Message.ID</summary>
        internal const int MsgIdLength = 16;

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1);

        internal byte[] ID { get; set; }
        internal IMessageDelegate Delegate { get; set; }

        private int _autoResponseDisabled;
        private int _responded;
        private string _idHexString;

        /// <summary>The message body byte array.</summary>
        /// <value>The message body byte array.</value>
        public byte[] Body { get; internal set; }

        /// <summary>The original timestamp when the message was published.</summary>
        /// <value>The original timestamp when the message was published.</value>
        public DateTime Timestamp { get; internal set; }

        /// <summary>The current attempt count to process this message. The first attempt is <c>1</c>.</summary>
        /// <value>The current attempt count to process this message. The first attempt is <c>1</c>.</value>
        public int Attempts { get; internal set; }

        /// <summary>The maximum number of attempts before nsqd will permanently fail this message.</summary>
        /// <value>The maximum number of attempts before nsqd will permanently fail this message.</value>
        public int MaxAttempts { get; internal set; }

        /// <summary>The nsqd address which sent this message.</summary>
        /// <value>The nsqd address which sent this message.</value>
        public string NsqdAddress { get; internal set; }

        /// <summary>Initializes a new instance of the <see cref="Message"/> class.</summary>
        /// <remarks>
        ///     The class is created in response to messages read from the TCP connection with nsqd. Typically you would not
        ///     need to instantiate this class yourself.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when either <paramref name="id"/> or <paramref name="body"/> are
        ///     <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="id"/> length is not 16.</exception>
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
        ///     <para>Disables the automatic response that would normally be sent when <see cref="IHandler.HandleMessage"/>
        ///     returns or throws.</para>
        ///     
        ///     <para>This is useful if you want to batch, buffer, or asynchronously respond to messages.</para>
        /// </summary>
        public void DisableAutoResponse()
        {
            // the CLR/CLI guarantees atomic reads/writes to ints
            // see: http://blogs.msdn.com/b/ericlippert/archive/2011/05/31/atomicity-volatility-and-immutability-are-different-part-two.aspx
            _autoResponseDisabled = 1;
        }

        /// <summary>
        ///     Indicates whether or not this message will be responded to automatically when
        ///     <see cref="IHandler.HandleMessage"/> returns or throws.
        /// </summary>
        /// <value><c>true</c> if automatic response is disabled; otherwise, <c>false</c>.</value>
        public bool IsAutoResponseDisabled
        {
            get { return (_autoResponseDisabled == 1); }
        }

        /// <summary>Indicates whether or not this message has been FIN'd or REQ'd.</summary>
        /// <value><c>true</c> if this message has been FIN'd or REQ'd; otherwise, <c>false</c>.</value>
        public bool HasResponded
        {
            get { return (_responded == 1); }
        }

        /// <summary>
        ///     Sends a FIN command to the nsqd which sent this message, indicating the message processed successfully.
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
        ///     <para>Sends a TOUCH command to the nsqd which sent this message, resetting the default message timeout.</para>
        ///     
        ///     <para>The server-default timeout is 60s; see <see cref="Config.MessageTimeout"/>.</para>
        ///     
        ///     <para>If FIN or REQ have already been sent for this message, calling <see cref="Touch"/> has no effect.</para>
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
        ///     <para>Sends a REQ command to the nsqd which sent this message, using the supplied delay.</para>
        ///     
        ///     <para>A delay of <c>null</c> will automatically calculate based on the number of attempts and the configured
        ///     <see cref="Config.DefaultRequeueDelay"/>.</para>
        ///     
        ///     <para>Using this method to respond triggers a backoff event.</para>
        /// </summary>
        /// <param name="delay">The minimum amount of time the message will be requeued.</param>
        public void Requeue(TimeSpan? delay = null)
        {
            doRequeue(delay, backoff: true);
        }

        /// <summary>
        ///     <para>Sends a REQ command to the nsqd which sent this message, using the supplied delay.</para>
        /// 
        ///     <para>A delay of <c>null</c> will automatically calculate based on the number of attempts and the configured
        ///     <see cref="Config.DefaultRequeueDelay"/>.</para>
        ///     
        ///     <para>Using this method to respond does not trigger a backoff event.</para>
        /// </summary>
        /// <param name="delay">The minimum amount of time the message will be requeued.</param>
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
            BackoffTriggered = backoff;
        }

        /// <summary>
        ///     Indicates whether this message triggered a backoff event, causing the <see cref="Consumer"/>
        ///     to slow its processing based on <see cref="Config.BackoffStrategy"/>.
        /// </summary>
        /// <value><c>true</c> if this message triggered a backoff event; otherwise, <c>false</c>.</value>
        public bool BackoffTriggered { get; private set; }

        /// <summary>
        ///     The minimum date/time the message will be requeued until; <c>null</c> indicates the message has not been
        ///     requeued.
        /// </summary>
        /// <value>
        ///     The minimum date/time the message will be requeued until; <c>null</c> indicates the message has not been
        ///     requeued.
        /// </value>
        public DateTime? RequeuedUntil { get; private set; }

        /// <summary>
        ///     <para>Encodes the message frame and body and writes it to the supplied <paramref name="writeStream"/>.</para>
        ///     
        ///     <para>It is suggested that the target <paramref name="writeStream"/> is buffered to avoid performing many
        ///     system calls.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="writeStream"/> is <c>null</c>.</exception>
        /// <param name="writeStream">The stream to write this message.</param>
        /// <returns>The number of bytes written to <paramref name="writeStream"/>.</returns>
        public Int64 WriteTo(Stream writeStream)
        {
            if (writeStream == null)
                throw new ArgumentNullException("writeStream");

            using (var writer = new BinaryWriter(writeStream))
            {
                ulong ns = (ulong)(Timestamp - _epoch).Ticks * 100;
                Binary.BigEndian.PutUint64(writer, ns); // 8 bytes
                Binary.BigEndian.PutUint16(writer, (ushort)Attempts); // 2 bytes

                writer.Write(ID); // MsgIdLength (16) bytes

                writer.Write(Body);
            }

            return 10 + MsgIdLength + Body.Length;
        }

        /// <summary>Decodes <paramref name="data"/> and creates a new <see cref="Message"/>.</summary>
        /// <exception cref="ArgumentNullException">Thrown <paramref name="data"/> is <c>null</c>.</exception>
        /// <param name="data">The fully encoded message.</param>
        /// <returns>The decoded message.</returns>
        public static Message DecodeMessage(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

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
        ///     <para>The message ID as a hexadecimal string.</para>
        ///     
        ///     <para>The message ID for a given message will be the same across channels; the message ID is created at the
        ///     topic level. If the message is requeued or times out it will retain the same message ID on future
        ///     attempts.</para>
        /// </summary>
        /// <value>The message ID as a hexadecimal string.</value>
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

    /// <summary>
    ///     Message is the fundamental data type containing the <see cref="Id"/>, <see cref="Body"/>, and metadata of a
    ///     message received from an nsqd instance.
    /// </summary>
    public interface IMessage
    {
        /// <summary>The message body byte array.</summary>
        /// <value>The message body byte array.</value>
        byte[] Body { get; }

        /// <summary>The original timestamp when the message was published.</summary>
        /// <value>The original timestamp when the message was published.</value>
        DateTime Timestamp { get; }

        /// <summary>The current attempt count to process this message. The first attempt is <c>1</c>.</summary>
        /// <value>The current attempt count to process this message. The first attempt is <c>1</c>.</value>
        int Attempts { get; }

        /// <summary>The maximum number of attempts before nsqd will permanently fail this message.</summary>
        /// <value>The maximum number of attempts before nsqd will permanently fail this message.</value>
        int MaxAttempts { get; }

        /// <summary>The nsqd address which sent this message.</summary>
        /// <value>The nsqd address which sent this message.</value>
        string NsqdAddress { get; }

        /// <summary>
        ///     <para>Disables the automatic response that would normally be sent when <see cref="IHandler.HandleMessage"/>
        ///     returns or throws.</para>
        ///     
        ///     <para>This is useful if you want to batch, buffer, or asynchronously respond to messages.</para>
        /// </summary>
        void DisableAutoResponse();

        /// <summary>
        ///     Indicates whether or not this message will be responded to automatically when
        ///     <see cref="IHandler.HandleMessage"/> returns or throws.
        /// </summary>
        /// <value><c>true</c> if automatic response is disabled; otherwise, <c>false</c>.</value>
        bool IsAutoResponseDisabled { get; }

        /// <summary>Indicates whether or not this message has been FIN'd or REQ'd.</summary>
        /// <value><c>true</c> if this message has been FIN'd or REQ'd; otherwise, <c>false</c>.</value>
        bool HasResponded { get; }

        /// <summary>
        ///     Sends a FIN command to the nsqd which sent this message, indicating the message processed successfully.
        /// </summary>
        void Finish();

        /// <summary>
        ///     <para>Sends a TOUCH command to the nsqd which sent this message, resetting the default message timeout.</para>
        ///     
        ///     <para>The server-default timeout is 60s; see <see cref="Config.MessageTimeout"/>.</para>
        ///     
        ///     <para>If FIN or REQ have already been sent for this message, calling <see cref="Touch"/> has no effect.</para>
        /// </summary>
        void Touch();

        /// <summary>
        ///     <para>Sends a REQ command to the nsqd which sent this message, using the supplied delay.</para>
        ///     
        ///     <para>A delay of <c>null</c> will automatically calculate based on the number of attempts and the configured
        ///     <see cref="Config.DefaultRequeueDelay"/>.</para>
        ///     
        ///     <para>Using this method to respond triggers a backoff event.</para>
        /// </summary>
        /// <param name="delay">The minimum amount of time the message will be requeued.</param>
        void Requeue(TimeSpan? delay = null);

        /// <summary>
        ///     <para>Sends a REQ command to the nsqd which sent this message, using the supplied delay.</para>
        /// 
        ///     <para>A delay of <c>null</c> will automatically calculate based on the number of attempts and the configured
        ///     <see cref="Config.DefaultRequeueDelay"/>.</para>
        ///     
        ///     <para>Using this method to respond does not trigger a backoff event.</para>
        /// </summary>
        /// <param name="delay">The minimum amount of time the message will be requeued.</param>
        void RequeueWithoutBackoff(TimeSpan? delay);

        /// <summary>
        ///     Indicates whether this message triggered a backoff event, causing the <see cref="Consumer"/>
        ///     to slow its processing based on <see cref="Config.BackoffStrategy"/>.
        /// </summary>
        /// <value><c>true</c> if this message triggered a backoff event; otherwise, <c>false</c>.</value>
        bool BackoffTriggered { get; }

        /// <summary>
        ///     The minimum date/time the message will be requeued until; <c>null</c> indicates the message has not been
        ///     requeued.
        /// </summary>
        /// <value>
        ///     The minimum date/time the message will be requeued until; <c>null</c> indicates the message has not been
        ///     requeued.
        /// </value>
        DateTime? RequeuedUntil { get; }

        /// <summary>
        ///     <para>Encodes the message frame and body and writes it to the supplied <paramref name="writeStream"/>.</para>
        ///     
        ///     <para>It is suggested that the target <paramref name="writeStream"/> is buffered to avoid performing many
        ///     system calls.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="writeStream"/> is <c>null</c>.</exception>
        /// <param name="writeStream">The stream to write this message.</param>
        /// <returns>The number of bytes written to <paramref name="writeStream"/>.</returns>
        Int64 WriteTo(Stream writeStream);

        /// <summary>
        ///     <para>The message ID as a hexadecimal string.</para>
        ///     
        ///     <para>The message ID for a given message will be the same across channels; the message ID is created at the
        ///     topic level. If the message is requeued or times out it will retain the same message ID on future
        ///     attempts.</para>
        /// </summary>
        /// <value>The message ID as a hexadecimal string.</value>
        string Id { get; }
    }
}
