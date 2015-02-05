using System;

namespace NsqSharp.Extensions
{
    /// <summary>
    /// Integer type extension methods.
    /// </summary>
    public static class IntExtensions
    {
        /// <summary>
        /// Reverses the endianness of the specified value
        /// </summary>
        public static UInt64 ReverseEndian(this UInt64 value)
        {
            return (
                ((value & 0xFF00000000000000) >> 56) |
                ((value & 0xFF000000000000) >> 40) |
                ((value & 0xFF0000000000) >> 24) |
                ((value & 0xFF00000000) >> 8) |
                ((value & 0xFF000000) << 8) |
                ((value & 0xFF0000) << 24) |
                ((value & 0xFF00) << 40) |
                ((value & 0xFF) << 56)
            );
        }

        /// <summary>
        /// Reverses the endianness of the specified value
        /// </summary>
        public static UInt32 ReverseEndian(this UInt32 value)
        {
            return (
                ((value & 0xFF000000) >> 24) |
                ((value & 0xFF0000) >> 8) |
                ((value & 0xFF00) << 8) |
                ((value & 0xFF) << 24)
            );
        }

        /// <summary>
        /// Reverses the endianness of the specified value
        /// </summary>
        public static UInt32 ReverseEndian(this Int32 value)
        {
            uint val = (uint)value;
            return (
                ((val & 0xFF000000) >> 24) |
                ((val & 0xFF0000) >> 8) |
                ((val & 0xFF00) << 8) |
                ((val & 0xFF) << 24)
            );
        }

        /// <summary>
        /// Reverses the endianness of the specified value
        /// </summary>
        public static UInt16 ReverseEndian(this UInt16 value)
        {
            return (UInt16)(
                ((value & 0xFF00) >> 8) |
                ((value & 0xFF) << 8)
            );
        }
    }
}
