using PointOfSale.Common.Nsq;
using PointOfSale.Handlers.Audit.Handlers;
using PointOfSale.Messages.Audit;

namespace PointOfSale.Handlers.Audit
{
    public class ChannelProvider : ChannelProviderBase
    {
        public ChannelProvider()
        {
            Add<TransportAuditHandler, MessageInformation>("audit");
        }
    }
}
