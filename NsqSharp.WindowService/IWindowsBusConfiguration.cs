using NsqSharp.Bus.Configuration;

namespace NsqSharp.WindowService 
{
    public interface IWindowsBusConfiguration : IBusConfiguration
    {
        /// <summary>
        /// <c>true</c> if the process is running in a console window.
        /// </summary>
        bool IsConsoleMode { get; }
    }
}