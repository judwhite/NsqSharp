using System;
using System.Linq;
using NsqSharp.Bus;
using PointOfSale.Messages.Products.Commands;
using PointOfSale.Messages.Products.Events;
using PointOfSale.Services.Products;

namespace PointOfSale.Handlers.ProductHandlers.Handlers
{
    public class GetProductsHandler : IHandleMessages<GetProductsCommand>
    {
        private readonly IBus _bus;
        private readonly IProductService _productService;

        public GetProductsHandler(IBus bus, IProductService productService)
        {
            if (bus == null)
                throw new ArgumentNullException("bus");
            if (productService == null)
                throw new ArgumentNullException("productService");

            _bus = bus;
            _productService = productService;
        }

        public void Handle(GetProductsCommand message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var productIds = _productService.GetProductIds();

            _bus.SendMulti(productIds.Select(id => new ProductIdFoundEvent { ProductId = id }));

            Console.WriteLine("Product Count: {0}", productIds.Count);
        }
    }
}
