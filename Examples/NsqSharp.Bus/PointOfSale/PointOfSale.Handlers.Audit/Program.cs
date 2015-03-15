using PointOfSale.Common;

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
