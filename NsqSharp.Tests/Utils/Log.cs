using System;
using System.Diagnostics;

namespace NsqSharp.Tests.Utils
{
    public static class log
    {
        public static void Printf(string format, params object[] arg)
        {
            if (arg == null || arg.Length == 0)
            {
                Debug.WriteLine(format);
            }
            else
            {
                Debug.WriteLine(format, arg);
            }
        }

        public static void Fatalf(string format, params object[] arg)
        {
            Printf(format, arg);

            Environment.Exit(-1);
        }
    }
}
