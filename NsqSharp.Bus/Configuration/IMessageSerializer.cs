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
        /// Deserializes the specified <paramref name="value"/> to an object.
        /// </summary>
        /// <typeparam name="T">The type of the deserialized object.</typeparam>
        /// <param name="value">The value to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        T Deserialize<T>(byte[] value);
    }
}
