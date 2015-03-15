using System.Configuration;

namespace PointOfSale.Common.Config
{
    internal class ConnectionStrings : IConnectionStrings
    {
        public ConnectionStrings()
        {
            var transportAuditConnectionString = ConfigurationManager.ConnectionStrings["TransportAudit"];
            if (transportAuditConnectionString != null)
            {
                TransportAudit = transportAuditConnectionString.ConnectionString;
            }
        }

        public string TransportAudit { get; private set; }
    }

    public interface IConnectionStrings
    {
        string TransportAudit { get; }
    }
}
