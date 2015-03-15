using System;
using System.Data;
using System.Data.SqlClient;
using NsqSharp.Bus;
using PointOfSale.Messages.Audit;

namespace PointOfSale.Handlers.Audit.Handlers
{
    public class TransportAuditHandler : IHandleMessages<MessageInformation>
    {
        public void Handle(MessageInformation info)
        {
            // TODO: Use app.config setting to control SQL logging

            /*using (var conn = new SqlConnection("Data Source=.\\SQLEXPRESS;Initial Catalog=NsqAudit;Integrated Security=SSPI"))
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
                cmd.Parameters.AddWithValue("@HandlerStart", info.Started);
                cmd.Parameters.AddWithValue("@HandlerFinish", info.Finished ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Success", info.Success ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FailedAction", info.FailedAction ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FailedReason", info.FailedReason ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Exception", info.FailedException ?? (object)DBNull.Value);

                cmd.ExecuteNonQuery();
            }*/
        }
    }
}
