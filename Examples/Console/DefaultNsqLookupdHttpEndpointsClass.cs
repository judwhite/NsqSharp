internal class DefaultNsqLookupdHttpEndpointsClass : IDefaultNsqLookupdHttpEndpoints
{
    public List<string> GetDefaultNsqLookupdHttpEndpoints()
    {
        return new List<string> { "http://localhost:4161" };
    }

    public void SetDefaultNsqLookupdHttpEndpoints(List<string> defaultNsqLookupdHttpEndpoints)
    {
        throw new NotImplementedException();
    }
}