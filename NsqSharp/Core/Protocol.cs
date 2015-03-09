using System;
using System.Text;
using System.Text.RegularExpressions;
using NsqSharp.Utils;

namespace NsqSharp.Core
{
    // https://github.com/bitly/go-nsq/blob/master/protocol.go

    /// <summary>
    /// Protocol
    /// </summary>
    public static partial class Protocol
    {
        /// <summary>
        /// MagicV1 is the initial identifier sent when connecting for V1 clients
        /// </summary>
        public static readonly byte[] MagicV1 = Encoding.UTF8.GetBytes("  V1");

        /// <summary>
        /// MagicV2 is the initial identifier sent when connecting for V2 clients
        /// </summary>
        public static readonly byte[] MagicV2 = Encoding.UTF8.GetBytes("  V2");
    }

    /// <summary>
    /// Frame types
    /// </summary>
    public enum FrameType
    {
        /// <summary>Response</summary>
        Response = 0,
        /// <summary>Error</summary>
        Error = 1,
        /// <summary>Message</summary>
        Message = 2
    }

    public static partial class Protocol
    {
        private static readonly Regex _validTopicChannelNameRegex =
            new Regex(@"^[\.a-zA-Z0-9_-]+(#ephemeral)?$", RegexOptions.Compiled);

        /// <summary>
        /// IsValidTopicName checks a topic name for correctness
        /// </summary>
        /// <param name="name">The topic name to check</param>
        /// <returns><c>true</c> if the topic name is valid; otherwise, <c>false</c></returns>
        public static bool IsValidTopicName(string name)
        {
            return isValidName(name);
        }

        /// <summary>
        /// IsValidChannelName checks a channel name for correctness
        /// </summary>
        /// <param name="name">The channel name to check</param>
        /// <returns><c>true</c> if the channel name is valid; otherwise, <c>false</c></returns>
        public static bool IsValidChannelName(string name)
        {
            return isValidName(name);
        }

        internal static bool isValidName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length > 64)
            {
                return false;
            }

            return _validTopicChannelNameRegex.IsMatch(name);
        }

        /// <summary>
        /// ReadResponse is a client-side utility function to read from the supplied Reader
        /// according to the NSQ protocol spec
        /// </summary>
        /// <param name="r">The stream to read from</param>
        /// <returns>The response as a byte array</returns>
        public static byte[] ReadResponse(IReader r)
        {
            if (r == null)
                throw new ArgumentNullException("r");

            // message size
            int msgSize = Binary.ReadInt32(r, Binary.BigEndian);
            byte[] data = new byte[msgSize];
            r.Read(data);
            return data;
        }

        /// <summary>
        /// UnpackResponse is a client-side utility function that unpacks serialized data
        /// according to NSQ protocol spec
        /// /// </summary>
        /// <param name="response">The response to unpack</param>
        /// <param name="frameType">The frame type.</param>
        /// <param name="body">The body.</param>
        public static void UnpackResponse(byte[] response, out FrameType frameType, out byte[] body)
        {
            if (response == null)
                throw new ArgumentNullException("response");
            if (response.Length < 4)
                throw new ArgumentException("length of response is too small", "response");

            frameType = (FrameType)Binary.BigEndian.Int32(response);
            body = new byte[response.Length - 4];
            Buffer.BlockCopy(response, 4, body, 0, body.Length);
        }

        /// <summary>
        /// ReadUnpackedResponse reads and parses data from the underlying
        /// TCP connection according to the NSQ TCP protocol spec and
        /// returns the frameType, data or error
        /// </summary>
        /// <param name="r">The reader to read from</param>
        /// <param name="frameType">The frame type.</param>
        /// <param name="body">The body.</param>
        public static void ReadUnpackedResponse(IReader r, out FrameType frameType, out byte[] body)
        {
            var resp = ReadResponse(r);
            UnpackResponse(resp, out frameType, out body);
        }
    }
}
