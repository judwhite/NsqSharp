using System;

namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Message serializer interface.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes the specified <paramref name="value"/> to a byte array.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized value.</returns>
        byte[] Serialize(object value);

        /// <summary>
        /// Deserializes the specified <paramref name="value"/> to an object of type <paramref name="type" />.
        /// </summary>
        /// <param name="type">The type of the deserialized object.</param>
        /// <param name="value">The value to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(Type type, byte[] value);
    }
}
