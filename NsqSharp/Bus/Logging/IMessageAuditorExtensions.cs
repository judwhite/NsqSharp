using System;

namespace NsqSharp.Bus.Logging
{
  using NsqSharp.Logging;

  internal static class IMessageAuditorExtensions
  {
    private static readonly ILog Log = LogProvider.GetCurrentClassLogger();

    public static void TryOnReceived(
      this IMessageAuditor messageAuditor,
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
        Log.ErrorException("messageAuditor.OnReceived", ex);
      }
    }

    public static void TryOnSucceeded(
      this IMessageAuditor messageAuditor,
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
        Log.ErrorException("messageAuditor.OnSucceeded", ex);
      }
    }

    public static void TryOnFailed(
      this IMessageAuditor messageAuditor,
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
        Log.ErrorException("messageAuditor.OnFailed", ex);
      }
    }
  }
}