using PointOfSale.Common.Nsq;
using PointOfSale.Handlers.ProductHandlers.Handlers;
using PointOfSale.Messages.Products.Commands;
using PointOfSale.Messages.Products.Events;

namespace PointOfSale.Handlers.ProductHandlers
{
    public class ChannelProvider : ChannelProviderBase
    {
        public ChannelProvider()
        {
            Add<GetProductsHandler, GetProductsCommand>("get-products");
            Add<GetProductDetailsHandler, ProductIdFoundEvent>("get-products-details");
        }
    }
}
