using System;
using System.IO;
using System.Text;

namespace NsqSharp.Bus.Utils
{
    /// <summary>
    /// Calculates a 32-bit Cyclic Redundancy Checksum (CRC) using the
    /// same polynomial used by Zip.
    /// </summary>
    public static class Crc32
    {
        private static readonly uint[] crc32Table = new uint[256];
        private const int BUFFER_SIZE = 1024;

        static Crc32()
        {
            unchecked
            {
                // This is the official polynomial used by CRC32 in PKZip.
                // Often the polynomial is shown reversed as 0x04C11DB7.
                const uint dwPolynomial = 0xEDB88320;

                for (uint i = 0; i < 256; i++)
                {
                    uint dwCrc = i;
                    for (uint j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        else
                            dwCrc >>= 1;
                    }
                    crc32Table[i] = dwCrc;
                }
            }
        }

        /// <summary>
        /// Returns the CRC32 Checksum of an input stream as a string.
        /// </summary>
        /// <param name="stream">Input stream.</param>
        /// <returns>CRC32 Checksum as a string.</returns>
        public static string Calculate(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return string.Format("{0:x8}", CalculateInt32(stream));
        }

        /// <summary>
        /// Returns the CRC32 Checksum of a byte array as a string.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <returns>CRC32 Checksum as a string.</returns>
        public static string Calculate(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            return string.Format("{0:x8}", CalculateInt32(data));
        }

        /// <summary>
        /// Returns the CRC32 Checksum of a string as a string.
        /// </summary>
        /// <param name="data">The string.</param>
        /// <returns>CRC32 Checksum as a string.</returns>
        public static string Calculate(string data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            return string.Format("{0:x8}", CalculateInt32(data));
        }

        /// <summary>
        /// Returns the CRC32 Checksum of an input stream as a four byte unsigned integer (UInt32).
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>CRC32 Checksum as a four byte unsigned integer (UInt32).</returns>
        public static uint CalculateInt32(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            unchecked
            {
                stream.Position = 0;
                uint crc32Result = 0xFFFFFFFF;
                byte[] buffer = new byte[BUFFER_SIZE];

                int count = stream.Read(buffer, 0, BUFFER_SIZE);
                while (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        crc32Result = ((crc32Result) >> 8) ^ crc32Table[(buffer[i]) ^
                                                                        ((crc32Result) & 0x000000FF)];
                    }
                    count = stream.Read(buffer, 0, BUFFER_SIZE);
                }

                return ~crc32Result;
            }
        }

        /// <summary>
        /// Returns the CRC32 Checksum of a byte array as a four byte unsigned integer (UInt32).
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <returns>CRC32 Checksum as a four byte unsigned integer (UInt32).</returns>
        public static uint CalculateInt32(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                return CalculateInt32(memoryStream);
            }
        }

        /// <summary>
        /// Returns the CRC32 Checksum of a string as a four byte unsigned integer (UInt32).
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <returns>CRC32 Checksum as a four byte unsigned integer (UInt32).</returns>
        public static uint CalculateInt32(string data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            return CalculateInt32(Encoding.UTF8.GetBytes(data));
        }
    }
}