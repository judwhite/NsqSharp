namespace NsqSharp.Tests.Utils
{
    public class NullLogger : ILogger
    {
        public void Output(int calldepth, string s)
        {
        }
    }
}
