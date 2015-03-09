using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using NsqSharp.Utils;

namespace NsqSharp.Core
{
    // https://github.com/bitly/go-nsq/blob/master/command.go

    /// <summary>
    /// Command represents a command from a client to an NSQ daemon
    /// </summary>
    public class Command
    {
        private static readonly BigEndian _bigEndian = Binary.BigEndian;

        private static readonly byte[] IDENTIFY_BYTES = Encoding.UTF8.GetBytes("IDENTIFY");
        private static readonly byte[] AUTH_BYTES = Encoding.UTF8.GetBytes("AUTH");
        private static readonly byte[] REGISTER_BYTES = Encoding.UTF8.GetBytes("REGISTER");
        private static readonly byte[] UNREGISTER_BYTES = Encoding.UTF8.GetBytes("UNREGISTER");
        private static readonly byte[] PING_BYTES = Encoding.UTF8.GetBytes("PING");
        private static readonly byte[] PUB_BYTES = Encoding.UTF8.GetBytes("PUB");
        private static readonly byte[] MPUB_BYTES = Encoding.UTF8.GetBytes("MPUB");
        private static readonly byte[] SUB_BYTES = Encoding.UTF8.GetBytes("SUB");
        private static readonly byte[] RDY_BYTES = Encoding.UTF8.GetBytes("RDY");
        private static readonly byte[] FIN_BYTES = Encoding.UTF8.GetBytes("FIN");
        private static readonly byte[] REQ_BYTES = Encoding.UTF8.GetBytes("REQ");
        private static readonly byte[] TOUCH_BYTES = Encoding.UTF8.GetBytes("TOUCH");
        private static readonly byte[] CLS_BYTES = Encoding.UTF8.GetBytes("CLS");
        private static readonly byte[] NOP_BYTES = Encoding.UTF8.GetBytes("NOP");

        private const byte byteSpace = (byte)' ';
        private const byte byteNewLine = (byte)'\n';

        /// <summary>Name</summary>
        public byte[] Name { get; set; }
        /// <summary>Params</summary>
        public ICollection<byte[]> Params { get; set; }
        /// <summary>Body</summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        public Command(byte[] name, string body, params string[] parameters)
            : this(name, body == null ? null : Encoding.UTF8.GetBytes(body), parameters)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        public Command(byte[] name, byte[] body, params string[] parameters)
        {
            Name = name;
            Body = body;

            if (parameters != null)
            {
                Params = new List<byte[]>(parameters.Length);
                foreach (var param in parameters)
                {
                    Params.Add(Encoding.UTF8.GetBytes(param));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command" /> class.
        /// </summary>
        public Command(byte[] name, byte[] body, ICollection<byte[]> parameters)
        {
            Name = name;
            Body = body;
            Params = parameters;
        }

        /// <summary>String returns the name and parameters of the Command</summary>
        public override string ToString()
        {
            string name = Encoding.UTF8.GetString(Name);

            if (Params != null && Params.Count > 0)
            {
                return string.Format("{0} {1}", name, string.Join(" ", Params.Select(p => Encoding.UTF8.GetString(p))));
            }

            return name;
        }

        internal int GetByteCount()
        {
            int size = Name.Length + 1 + (Body == null ? 0 : Body.Length + 4);
            if (Params != null)
            {
                foreach (var param in Params)
                {
                    size += param.Length + 1;
                }
            }

            return size;
        }

        /// <summary>
        /// WriteTo implements the WriterTo interface and
        /// serializes the Command to the supplied Writer.
        ///
        /// It is suggested that the target Writer is buffered
        /// to avoid performing many system calls.
        /// </summary>
        public long WriteTo(IWriter w)
        {
            var buf = new byte[GetByteCount()];
            return WriteTo(w, buf);
        }

        /// <summary>
        /// WriteTo implements the WriterTo interface and
        /// serializes the Command to the supplied Writer.
        ///
        /// It is suggested that the target Writer is buffered
        /// to avoid performing many system calls.
        /// </summary>
        internal long WriteTo(IWriter w, byte[] buf)
        {
            int j = 0;

            int count = Name.Length;
            Buffer.BlockCopy(Name, 0, buf, j, count);
            j += count;

            if (Params != null)
            {
                foreach (var param in Params)
                {
                    buf[j++] = byteSpace;
                    count = param.Length;
                    Buffer.BlockCopy(param, 0, buf, j, count);
                    j += count;
                }
            }

            buf[j++] = byteNewLine;

            if (Body != null)
            {
                _bigEndian.PutUint32(buf, Body.Length, j);
                j += 4;

                count = Body.Length;
                Buffer.BlockCopy(Body, 0, buf, j, count);
                j += count;
            }

            return w.Write(buf, 0, j);
        }

        /// <summary>
        /// Identify creates a new Command to provide information about the client.  After connecting,
        /// it is generally the first message sent.
        ///
        /// The supplied map is marshaled into JSON to provide some flexibility
        /// for this command to evolve over time.
        ///
        /// See http://bitly.github.io/nsq/clients/tcp_protocol_spec.html#identify for information
        /// on the supported options
        /// </summary>
        public static Command Identify(IdentifyRequest request)
        {
            byte[] body;

            var serializer = new DataContractJsonSerializer(typeof(IdentifyRequest));

            using (var memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, request);
                body = memoryStream.ToArray();
            }

            return new Command(IDENTIFY_BYTES, body);
        }

        /// <summary>
        /// Auth sends credentials for authentication
        ///
        /// After `Identify`, this is usually the first message sent, if auth is used.
        /// </summary>
        public static Command Auth(string secret)
        {
            return new Command(AUTH_BYTES, secret);
        }

        /// <summary>
        /// Register creates a new Command to add a topic/channel for the connected nsqd
        /// </summary>
        public static Command Register(string topic, string channel)
        {
            return new Command(REGISTER_BYTES, (byte[])null, topic, channel);
        }

        /// <summary>
        /// UnRegister creates a new Command to remove a topic/channel for the connected nsqd
        /// </summary>
        public static Command UnRegister(string topic, string channel)
        {
            return new Command(UNREGISTER_BYTES, (byte[])null, topic, channel);
        }

        /// <summary>
        /// Ping creates a new Command to keep-alive the state of all the
        /// announced topic/channels for a given client
        /// </summary>
        public static Command Ping()
        {
            return new Command(PING_BYTES, (byte[])null);
        }

        /// <summary>
        /// Publish creates a new Command to write a message to a given topic
        /// </summary>
        public static Command Publish(string topic, byte[] body)
        {
            return new Command(PUB_BYTES, body, topic);
        }

        /// <summary>
        /// MultiPublish creates a new Command to write more than one message to a given topic.
        /// This is useful for high-throughput situations to avoid roundtrips and saturate the pipe.
        /// </summary>
        public static Command MultiPublish(string topic, ICollection<byte[]> bodies)
        {
            if (bodies == null)
                throw new ArgumentNullException("bodies");

            int num = bodies.Count;
            byte[] body;

            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    Binary.BigEndian.PutUint32(binaryWriter, num);
                    foreach (var b in bodies)
                    {
                        Binary.BigEndian.PutUint32(binaryWriter, b.Length);
                        binaryWriter.Write(b);
                    }
                }

                body = memoryStream.ToArray();
            }

            return new Command(MPUB_BYTES, body, topic);
        }

        /// <summary>
        /// Subscribe creates a new Command to subscribe to the given topic/channel
        /// </summary>
        public static Command Subscribe(string topic, string channel)
        {
            return new Command(SUB_BYTES, (byte[])null, topic, channel);
        }

        /// <summary>
        /// Ready creates a new Command to specify
        /// the number of messages a client is willing to receive
        /// </summary>
        public static Command Ready(long count)
        {
            return new Command(RDY_BYTES, (byte[])null, count.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Finish creates a new Command to indiciate that
        /// a given message (by id) has been processed successfully
        /// </summary>
        public static Command Finish(byte[] id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (id.Length != Message.MsgIdLength)
                throw new ArgumentOutOfRangeException("id", id.Length, string.Format("id length must be {0} bytes", Message.MsgIdLength));

            return new Command(FIN_BYTES, null, new List<byte[]> { id });
        }

        /// <summary>
        /// Requeue creates a new Command to indicate that
        /// a given message (by id) should be requeued after the given delay
        /// NOTE: a delay of 0 indicates immediate requeue
        /// </summary>
        public static Command Requeue(byte[] id, TimeSpan delay)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (id.Length != Message.MsgIdLength)
                throw new ArgumentOutOfRangeException("id", id.Length, string.Format("id length must be {0} bytes", Message.MsgIdLength));

            int delayMilliseconds = (int)delay.TotalMilliseconds;

            var parameters = new List<byte[]>();
            parameters.Add(id);
            parameters.Add(Encoding.UTF8.GetBytes(delayMilliseconds.ToString(CultureInfo.InvariantCulture)));

            return new Command(REQ_BYTES, null, parameters);
        }

        /// <summary>
        /// Touch creates a new Command to reset the timeout for
        /// a given message (by id)
        /// </summary>
        public static Command Touch(byte[] id)
        {
            if (id == null)
                throw new ArgumentNullException("id");
            if (id.Length != Message.MsgIdLength)
                throw new ArgumentOutOfRangeException("id", id.Length, string.Format("id length must be {0} bytes", Message.MsgIdLength));

            return new Command(TOUCH_BYTES, null, new List<byte[]> { id });
        }

        /// <summary>
        /// StartClose creates a new Command to indicate that the
        /// client would like to start a close cycle.  nsqd will no longer
        /// send messages to a client in this state and the client is expected
        /// finish pending messages and close the connection
        /// </summary>
        public static Command StartClose()
        {
            return new Command(CLS_BYTES, (byte[])null);
        }

        /// <summary>
        /// Nop creates a new Command that has no effect server side.
        /// Commonly used to respond to heartbeats
        /// </summary>
        public static Command Nop()
        {
            return new Command(NOP_BYTES, (byte[])null);
        }
    }
}
