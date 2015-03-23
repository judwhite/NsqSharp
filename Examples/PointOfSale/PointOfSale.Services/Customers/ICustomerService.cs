using System.Collections.ObjectModel;
using PointOfSale.Services.Customers.Models;

namespace PointOfSale.Services.Customers
{
    public interface ICustomerService
    {
        Collection<int> GetCustomerIds();
        Customer GetCustomer(int customerId);
    }
}
