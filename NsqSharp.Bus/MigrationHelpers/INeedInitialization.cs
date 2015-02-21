namespace NsqSharp.Bus.Configuration
{
    /// <summary>
    /// Used for migration. Synonym for <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public interface INeedInitialization : IConfigureThisEndpoint
    {
    }
}
