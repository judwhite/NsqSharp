using System;
using NsqSharp.Attributes;

namespace NsqSharp.Extensions
{
    internal static class OptAttributeExtensions
    {
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
