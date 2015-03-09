using NsqSharp.Utils;

namespace NsqSharp.Tests.Utils
{
    public class NullLogger : ILogger
    {
        public void Output(string s)
        {
        }
    }
}
