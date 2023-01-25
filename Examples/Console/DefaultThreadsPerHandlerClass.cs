internal class DefaultThreadsPerHandlerClass : IDefaultThreadsPerHandler
{
    public int GetDefaultThreadsPerHandler()
    {
        return 1;
    }

    public void SetDefaultThreadsPerHandler(int defaultThreadsPerHandler)
    {
        throw new NotImplementedException();
    }
}