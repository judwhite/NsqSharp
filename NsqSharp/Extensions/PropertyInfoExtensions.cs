using System;
using System.Linq;
using System.Reflection;

namespace NsqSharp.Extensions
{
    /// <summary>
    /// <see cref="PropertyInfo"/> extensions.
    /// </summary>
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// Gets the specified <paramref name="propertyInfo"/> attribute of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <param name="propertyInfo">The <see cref="PropertyInfo" />.</param>
        /// <returns>The attribute, or <c>null</c>.</returns>
        public static T Get<T>(this PropertyInfo propertyInfo)
            where T : Attribute
        {
            return propertyInfo.GetCustomAttributes(typeof(T), inherit: false).SingleOrDefault() as T;
        }
    }
}
