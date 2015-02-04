using System;
using System.Threading.Tasks;

namespace NsqSharp.Go
{
    internal static class GoFunc
    {
        public static void Run(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            Task.Factory.StartNew(action);
        }
    }
}
