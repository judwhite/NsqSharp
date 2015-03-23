using System;
using System.Diagnostics;
using NsqSharp.Bus;
using PointOfSale.Messages.Products.Events;
using PointOfSale.Services.Products;

namespace PointOfSale.Handlers.ProductHandlers.Handlers
{
    public class GetProductDetailsHandler : IHandleMessages<ProductIdFoundEvent>
    {
        private readonly IProductService _productService;

        public GetProductDetailsHandler(IProductService productService)
        {
            if (productService == null)
                throw new ArgumentNullException("productService");

            _productService = productService;
        }

        public void Handle(ProductIdFoundEvent message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            var product = _productService.GetProduct(message.ProductId);

            Trace.WriteLine(string.Format("Product: Id: {0} Name: {1} Price: {2:c}",
                product.ProductId, product.Name, product.Price));
        }
    }
}
