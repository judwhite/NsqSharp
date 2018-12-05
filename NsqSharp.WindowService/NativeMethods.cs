using System;
using System.Runtime.InteropServices;

namespace NsqSharp.WindowService
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(HandlerRoutineCallback handler, bool add);
    }

    internal delegate bool HandlerRoutineCallback(CtrlType dwCtrlType);

    internal enum CtrlType
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }
}
