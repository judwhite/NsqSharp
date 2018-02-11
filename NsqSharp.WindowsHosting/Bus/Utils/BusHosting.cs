using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NsqSharp.Bus.Utils;
namespace NsqSharp.Bus
{

    public static class BusHosting
    {
        /// <summary>
        /// <c>true</c> if the process is running in a console window.
        /// </summary>
        public static bool IsConsoleMode 
        {
            get
            {
                return (NativeMethods.GetConsoleWindow() != IntPtr.Zero);
            }
        }
    }
}
