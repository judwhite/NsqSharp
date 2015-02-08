using System;
using System.Security.Cryptography;

namespace NsqSharp.Extensions
{
    /// <summary>
    /// RNGCryptoServiceProvider extensions.
    /// </summary>
    public static class RNGCryptoServiceProviderExtensions
    {
        /// <summary>
        /// Gets a cryptographically secure double.
        /// </summary>
        /// <param name="rng">The <see cref="RNGCryptoServiceProvider"/>.</param>
        /// <returns>A cryptographically secure double.</returns>
        public static double Float64(this RNGCryptoServiceProvider rng)
        {
            byte[] value = new byte[8];
            rng.GetBytes(value);
            double dbl = (double)BitConverter.ToUInt64(value, 0) / ulong.MaxValue;
            return dbl;
        }
    }
}
