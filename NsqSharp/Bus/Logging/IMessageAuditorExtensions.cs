using System;
using NsqSharp.Core;

namespace NsqSharp.Bus.Logging
{
    internal static class IMessageAuditorExtensions
    {
        public static void TryOnReceived(
            this IMessageAuditor messageAuditor,
            Core.ILogger logger,
            IBus bus,
            MessageInformation messageInformation
        )
        {
            try
            {
                messageAuditor.OnReceived(bus, messageInformation);
            }
            catch (Exception ex)
            {
                logger.Output(Core.LogLevel.Error, string.Format("messageAuditor.OnReceived - {0}", ex));
            }
        }

        public static void TryOnSucceeded(
            this IMessageAuditor messageAuditor,
            Core.ILogger logger,
            IBus bus,
            MessageInformation messageInformation
        )
        {
            try
            {
                messageAuditor.OnSucceeded(bus, messageInformation);
            }
            catch (Exception ex)
            {
                logger.Output(Core.LogLevel.Error, string.Format("messageAuditor.OnSucceeded - {0}", ex));
            }
        }

        public static void TryOnFailed(
            this IMessageAuditor messageAuditor,
            Core.ILogger logger,
            IBus bus,
            FailedMessageInformation failedMessageInformation
        )
        {
            try
            {
                messageAuditor.OnFailed(bus, failedMessageInformation);
            }
            catch (Exception ex)
            {
                logger.Output(Core.LogLevel.Error, string.Format("messageAuditor.OnFailed - {0}", ex));
            }
        }
    }
}
