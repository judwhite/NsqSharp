namespace NsqSharp.Bus
{
    /// <summary>
    /// Configures a new bus.
    /// </summary>
    public static class Configure
    {
        /// <summary>
        /// Configures a new <see cref="IBus"/> using the specified <paramref name="busType"/>.
        /// </summary>
        /// <param name="busType">The <see cref="BusType"/>.</param>
        /// <returns>The <see cref="IConfiguration"/> for this bus.</returns>
        public static IConfiguration With(BusType busType)
        {
            return new Configuration(busType);
        }
    }
}
