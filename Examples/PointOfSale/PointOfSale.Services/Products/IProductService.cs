using System.Collections.ObjectModel;
using PointOfSale.Services.Products.Models;

namespace PointOfSale.Services.Products
{
    public interface IProductService
    {
        Collection<int> GetProductIds();
        Product GetProduct(int productId);
    }
}
