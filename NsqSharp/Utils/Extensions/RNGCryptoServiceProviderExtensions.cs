using System;
using System.Security.Cryptography;

namespace NsqSharp.Utils.Extensions
{
    /// <summary>
    /// <see cref="RNGCryptoServiceProvider"/> extension methods.
    /// </summary>
    public static class RNGCryptoServiceProviderExtensions
    {
        /// <summary>
        /// Gets a cryptographically secure double in [0,1).
        /// </summary>
        /// <param name="rng">The <see cref="RNGCryptoServiceProvider"/>.</param>
        /// <returns>A cryptographically secure double.</returns>
        public static double Float64(this RNGCryptoServiceProvider rng)
        {
            byte[] value = new byte[8];
            rng.GetBytes(value);
            return (double)Math.Min(UInt64.MaxValue - 1, BitConverter.ToUInt64(value, 0)) / ulong.MaxValue;
        }

        /// <summary>
        /// Intn returns, as an int, a non-negative cryptographically secure random number in [0,<paramref name="n"/>).
        /// </summary>
        /// <param name="rng">The <see cref="RNGCryptoServiceProvider"/>.</param>
        /// <param name="n">The exclusive upper bound. Must be > 0.</param>
        /// <returns>A cryptographically secure integer in [0,<paramref name="n"/>).</returns>
        public static int Intn(this RNGCryptoServiceProvider rng, int n)
        {
            if (n <= 0)
                throw new ArgumentOutOfRangeException("n", n, "n must be > 0");

            return Int(rng) % n;
        }

        /// <summary>
        /// Int returns a non-negative cryptographically secure random number.
        /// </summary>
        /// <param name="rng">The <see cref="RNGCryptoServiceProvider"/>.</param>
        /// <returns>A cryptographically secure integer.</returns>
        public static int Int(this RNGCryptoServiceProvider rng)
        {
            byte[] value = new byte[4];
            rng.GetBytes(value);
            return Math.Abs(BitConverter.ToInt32(value, 0));
        }
    }
}
