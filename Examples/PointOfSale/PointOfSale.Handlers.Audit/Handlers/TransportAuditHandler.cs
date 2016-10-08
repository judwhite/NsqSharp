using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using NsqSharp.Bus;
using PointOfSale.Common.Config;
using PointOfSale.Messages.Audit;

namespace PointOfSale.Handlers.Audit.Handlers
{
    public class TransportAuditHandler : IHandleMessages<MessageInformation>
    {
        private readonly IAppSettings _appSettings;
        private readonly IConnectionStrings _connectionStrings;

        public TransportAuditHandler(IAppSettings appSettings, IConnectionStrings connectionStrings)
        {
            _appSettings = appSettings;
            _connectionStrings = connectionStrings;
        }

        public void Handle(MessageInformation info)
        {
            if (!_appSettings.UseSql)
            {
                if (info.Success == null)
                {
                    Trace.WriteLine(string.Format("message id {0} received {1}", info.MessageId, info.MessageBody));
                }
                else if (info.Success == true)
                {
                    Trace.WriteLine(string.Format("message id {0} succeeded", info.MessageId));
                }
                else
                {
                    string logEntry = string.Format(
                        "id:{0} action:{1} reason:{2} topic:{3} channel:{4} msg:{5} ex:{6}",
                        info.MessageId, info.FailedAction, info.FailedReason, info.Topic, info.Channel, info.MessageBody,
                        info.FailedException
                    );

                    Trace.TraceError(logEntry);
                }
            }
            else
            {
                using (var conn = new SqlConnection(_connectionStrings.TransportAudit))
                using (var cmd = new SqlCommand("dbo.spTransportAudit_InsertUpdate", conn))
                {
                    conn.Open();

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@TransportAuditId", info.UniqueIdentifier);
                    cmd.Parameters.AddWithValue("@Topic", info.Topic);
                    cmd.Parameters.AddWithValue("@Channel", info.Channel);
                    cmd.Parameters.AddWithValue("@HandlerType", info.HandlerType);
                    cmd.Parameters.AddWithValue("@MessageType", info.MessageType);
                    cmd.Parameters.AddWithValue("@MessageId", info.MessageId);
                    cmd.Parameters.AddWithValue("@Attempt", info.MessageAttempt);
                    cmd.Parameters.AddWithValue("@NsqdAddress", info.MessageNsqdAddress);
                    cmd.Parameters.AddWithValue("@Body", info.MessageBody);
                    cmd.Parameters.AddWithValue("@PublishTimestamp", info.MessageOriginalTimestamp);
                    cmd.Parameters.AddWithValue("@HandlerStarted", info.Started);
                    cmd.Parameters.AddWithValue("@HandlerFinished", info.Finished ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Success", info.Success ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FailedAction", info.FailedAction ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FailedReason", info.FailedReason ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Exception", info.FailedException ?? (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
