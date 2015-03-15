using PointOfSale.Common.Nsq;

namespace PointOfSale.Handlers.Audit
{
    class Program
    {
        public static void Main()
        {
            PointOfSaleBus.Start(new ChannelProvider());
        }
    }
}
