using System;
using System.IO;

namespace NsqSharp.Utils
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
            r.Read(buf);
            return order.Int32(buf);
        }
    }

    /// <summary>
    /// Binary.BigEndian 
    /// </summary>
    public class BigEndian : IByteOrder
    {
        /// <summary>
        /// Fills a byte array with a <see cref="T:System.UInt64"/> using big endian ordering.
        /// </summary>
        public void PutUint64(byte[] b, UInt64 v)
        {
            b[0] = (byte)(v >> 56);
            b[1] = (byte)((v >> 48) & 0xFF);
            b[2] = (byte)((v >> 40) & 0xFF);
            b[3] = (byte)((v >> 32) & 0xFF);
            b[4] = (byte)((v >> 24) & 0xFF);
            b[5] = (byte)((v >> 16) & 0xFF);
            b[6] = (byte)((v >> 8) & 0xFF);
            b[7] = (byte)(v & 0xFF);
        }

        /// <summary>
        /// Writes a <see cref="T:System.UInt64"/> using big endian ordering.
        /// </summary>
        public void PutUint64(BinaryWriter w, UInt64 v)
        {
            w.Write((byte)(v >> 56));
            w.Write((byte)((v >> 48) & 0xFF));
            w.Write((byte)((v >> 40) & 0xFF));
            w.Write((byte)((v >> 32) & 0xFF));
            w.Write((byte)((v >> 24) & 0xFF));
            w.Write((byte)((v >> 16) & 0xFF));
            w.Write((byte)((v >> 8) & 0xFF));
            w.Write((byte)(v & 0xFF));
        }

        /// <summary>
        /// Fills a byte array with a <see cref="T:System.UInt32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(byte[] b, UInt32 v)
        {
            b[0] = (byte)(v >> 24);
            b[1] = (byte)((v >> 16) & 0xFF);
            b[2] = (byte)((v >> 8) & 0xFF);
            b[3] = (byte)(v & 0xFF);
        }

        /// <summary>
        /// Writes a <see cref="T:System.UInt32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(BinaryWriter w, UInt32 v)
        {
            w.Write((byte)(v >> 24));
            w.Write((byte)((v >> 16) & 0xFF));
            w.Write((byte)((v >> 8) & 0xFF));
            w.Write((byte)(v & 0xFF));
        }

        /// <summary>
        /// Fills a byte array with a <see cref="T:System.Int32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(byte[] b, Int32 v)
        {
            PutUint32(b, v, 0);
        }

        /// <summary>
        /// Writes a <see cref="T:System.Int32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(BinaryWriter w, Int32 v)
        {
            w.Write((byte)(v >> 24));
            w.Write((byte)((v >> 16) & 0xFF));
            w.Write((byte)((v >> 8) & 0xFF));
            w.Write((byte)(v & 0xFF));
        }

        /// <summary>
        /// Fills a byte array with a <see cref="T:System.Int32"/> using big endian ordering.
        /// </summary>
        public void PutUint32(byte[] b, Int32 v, int offset)
        {
            b[offset] = (byte)(v >> 24);
            b[offset + 1] = (byte)((v >> 16) & 0xFF);
            b[offset + 2] = (byte)((v >> 8) & 0xFF);
            b[offset + 3] = (byte)(v & 0xFF);
        }

        /// <summary>
        /// Fills a byte array with a <see cref="T:System.UInt16"/> using big endian ordering.
        /// </summary>
        public void PutUint16(byte[] b, UInt16 v)
        {
            b[0] = (byte)((v >> 8) & 0xFF);
            b[1] = (byte)(v & 0xFF);
        }

        /// <summary>
        /// Fills a byte array with a <see cref="T:System.UInt16"/> using big endian ordering.
        /// </summary>
        public void PutUint16(BinaryWriter w, UInt16 v)
        {
            w.Write((byte)((v >> 8) & 0xFF));
            w.Write((byte)(v & 0xFF));
        }

        /// <summary>
        /// Reads a byte array into a new <see cref="T:System.Int32"/> using big endian ordering.
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

        /// <summary>
        /// Reads a byte array into a new <see cref="T:System.Int64"/> using big endian ordering.
        /// Warning: Will reorder byte array if <see cref="BitConverter.IsLittleEndian"/> is <c>true</c>.
        /// </summary>
        private UInt64 UInt64(byte[] b)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b);
            return BitConverter.ToUInt64(b, 0);
        }

        /// <summary>
        /// Reads a a new <see cref="T:System.UInt64"/> using big endian ordering.
        /// </summary>
        public UInt64 UInt64(BinaryReader r)
        {
            return UInt64(r.ReadBytes(8));
        }

        /// <summary>
        /// Reads a byte array into a new <see cref="T:System.UInt16"/> using big endian ordering.
        /// </summary>
        public UInt16 UInt16(byte[] b)
        {
            ushort value = (ushort)(
                (b[0] << 8) |
                (b[1])
            );

            return value;
        }

        /// <summary>
        /// Reads a a new <see cref="T:System.UInt16"/> using big endian ordering.
        /// </summary>
        public UInt16 UInt16(BinaryReader r)
        {
            return UInt16(r.ReadBytes(2));
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
        /// Fills a byte array with a <see cref="Int32"/> using big endian ordering.
        /// </summary>
        void PutUint32(byte[] b, Int32 v, int offset);

        /// <summary>
        /// Reads a byte array into a new <see cref="Int32"/> using big endian ordering.
        /// </summary>
        int Int32(byte[] b);
    }
}
