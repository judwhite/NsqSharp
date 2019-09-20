using NsqSharp.Bus.Configuration;
using NsqSharp.Bus.Configuration.Providers;
using NsqSharp.Bus.Logging;
using NsqSharp.Core;
using System;

namespace NsqSharp.WindowService
{
    public class WindowsBusConfiguration : BusConfiguration, IWindowsBusConfiguration
    {
        public WindowsBusConfiguration(IObjectBuilder dependencyInjectionContainer,
            IMessageSerializer defaultMessageSerializer,
            IMessageAuditor messageAuditor,
            IMessageTypeToTopicProvider messageTypeToTopicProvider,
            IHandlerTypeToChannelProvider handlerTypeToChannelProvider,
            string[] defaultNsqLookupdHttpEndpoints,
            int defaultThreadsPerHandler,
            Config nsqConfig = null,
            IBusStateChangedHandler busStateChangedHandler = null,
            ILogger nsqLogger = null,
            bool preCreateTopicsAndChannels = false,
            IMessageMutator messageMutator = null,
            IMessageTopicRouter messageTopicRouter = null,
            INsqdPublisher nsqdPublisher = null,
            bool logOnProcessCrash = true) : base(
                dependencyInjectionContainer,
                defaultMessageSerializer,
                messageAuditor,
                messageTypeToTopicProvider,
                handlerTypeToChannelProvider,
                defaultNsqLookupdHttpEndpoints,
                defaultThreadsPerHandler,
                nsqConfig = null,
                busStateChangedHandler = null,
                nsqLogger = null,
                preCreateTopicsAndChannels = false,
                messageMutator = null,
                messageTopicRouter = null,
                nsqdPublisher = null,
                logOnProcessCrash = true)
        { }

        /// <summary>
        /// <c>true</c> if the process is running in a console window.
        /// </summary>
        public bool IsConsoleMode => (NativeMethods.GetConsoleWindow() != IntPtr.Zero);
    }
}
