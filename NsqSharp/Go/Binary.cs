using System;

namespace NsqSharp.Go
{
    /// <summary>
    /// Binary package. https://golang.org/src/encoding/binary/binary.go
    /// </summary>
    public static class Binary
    {
        /// <summary>
        /// BigEndian is the big-endian implementation of ByteOrder.
        /// </summary>
        public static readonly BigEndian BigEndian = new BigEndian();

        /// <summary>
        /// Read reads structured binary data from <paramref name="r"/>.
        /// Bytes read from <paramref name="r"/> are decoded using the
        /// specified byte <paramref name="order"/> and written to successive
        /// fields of the data.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="order">The byte order.</param>
        public static int ReadInt32(IReader r, IByteOrder order)
        {
            // NOTE: Departing from "binary.Read", don't want to box/unbox for this low-level call

            var buf = new byte[4];
            r.Read(buf); // TODO: What if return value != buf.Length ?
            return order.Int32(buf);
        }
    }

    /// <summary>
    /// Binary.BigEndian 
    /// </summary>
    public class BigEndian : IByteOrder
    {
        /// <summary>
        /// Fills a byte array with a <see cref="UInt32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(byte[] b, UInt32 v)
        {
            b[0] = (byte)(v >> 24);
            b[1] = (byte)((v >> 16) & 0xFF);
            b[2] = (byte)((v >> 8) & 0xFF);
            b[3] = (byte)(v & 0xFF);
        }

        /// <summary>
        /// Fills a byte array with a <see cref="Int32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(byte[] b, Int32 v)
        {
            b[0] = (byte)(v >> 24);
            b[1] = (byte)((v >> 16) & 0xFF);
            b[2] = (byte)((v >> 8) & 0xFF);
            b[3] = (byte)(v & 0xFF);
        }

        /// <summary>
        /// Reads a byte array into a new <see cref="Int32"/> using big endian ordering.
        /// </summary>
        public Int32 Int32(byte[] b)
        {
            int value =
                (b[0] << 24) |
                (b[1] << 16) |
                (b[2] << 8) |
                (b[3]);

            return value;
        }
    }

    /// <summary>
    /// A ByteOrder specifies how to convert byte sequences into
    /// 16-, 32-, or 64-bit unsigned integers.
    /// </summary>
    public interface IByteOrder
    {
        /// <summary>
        /// Fills a byte array with a <see cref="UInt32"/> using big endian ordering.
        /// </summary>
        void PutUint32(byte[] b, UInt32 v);

        /// <summary>
        /// Fills a byte array with a <see cref="Int32"/> using big endian ordering.
        /// </summary>
        void PutUint32(byte[] b, Int32 v);

        /// <summary>
        /// Reads a byte array into a new <see cref="Int32"/> using big endian ordering.
        /// </summary>
        int Int32(byte[] b);
    }
}
