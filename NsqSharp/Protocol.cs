using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NsqSharp.Extensions;

namespace NsqSharp
{
    // https://github.com/bitly/go-nsq/blob/v1.0.2/protocol.go

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
        private static readonly Regex _validTopicChannelNameRegex = new Regex(@"^[\.a-zA-Z0-9_-]+(#ephemeral)?$", RegexOptions.Compiled);

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
        public static byte[] ReadResponse(Stream r)
        {
            if (r == null)
                throw new ArgumentNullException("r");

            using (var streamReader = new BinaryReader(r))
            {
                int msgSize = (int)streamReader.ReadUInt32().AsBigEndian();
                return streamReader.ReadBytes(msgSize);
            }
        }

        /// <summary>
        /// UnpackResponse is a client-side utility function that unpacks serialized data
        /// according to NSQ protocol spec
        /// /// </summary>
        /// <param name="response">The response to unpack</param>
        /// <returns>A tuple containing the <see cref="FrameType"/> and body</returns>
        public static Tuple<FrameType, byte[]> UnpackResponse(byte[] response)
        {
            if (response == null)
                throw new ArgumentNullException("response");
            if (response.Length < 4)
                throw new ArgumentException("length of response is too small", "response");

            int frameType = (int)BitConverter.ToUInt32(response, 0).AsBigEndian();
            byte[] body = new byte[response.Length - 4];
            Buffer.BlockCopy(response, 4, body, 0, body.Length);

            return new Tuple<FrameType, byte[]>((FrameType)frameType, body);
        }

        /// <summary>
        /// ReadUnpackedResponse reads and parses data from the underlying
        /// TCP connection according to the NSQ TCP protocol spec and
        /// returns the frameType, data or error
        /// </summary>
        /// <param name="r">The stream to read from</param>
        /// <returns>A tuple containing the <see cref="FrameType"/> and body</returns>
        public static Tuple<FrameType, byte[]> ReadUnpackedResponse(Stream r)
        {
            // NOTE: Implementation changed from original Go client. Repeats logic in ReadResponse and UnpackResponse to avoid allocating more byte arrays than necessary
            // (orig. implementations works for slices, not arrays)

            if (r == null)
                throw new ArgumentNullException("r");

            using (var streamReader = new BinaryReader(r))
            {
                int msgSize = (int)streamReader.ReadUInt32().AsBigEndian();
                int frameType = (int)streamReader.ReadUInt32().AsBigEndian();
                byte[] body = streamReader.ReadBytes(msgSize - 4);

                return new Tuple<FrameType, byte[]>((FrameType)frameType, body);
            }
        }
    }
}
