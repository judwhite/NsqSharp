using System;
using NsqSharp.Attributes;

namespace NsqSharp.Extensions
{
    /// <summary>
    /// <see cref="OptAttribute"/> extension methods.
    /// </summary>
    public static class OptAttributeExtensions
    {
        /// <summary>
        /// Coerce a value to the specified <paramref name="targetType"/>. Supports Duration and Bool string/int formats.
        /// </summary>
        /// <param name="opt">The <see cref="OptAttribute.Name"/> to throw in the exception if coerce fails.</param>
        /// <param name="value">The value to coerce.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>The value as the <paramref name="targetType"/>.</returns>
        public static object Coerce(this OptAttribute opt, object value, Type targetType)
        {
            try
            {
                return value.Coerce(targetType);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("failed to coerce option {0}", opt.Name), ex);
            }
        }
    }
}
