using System.Collections.ObjectModel;
using PointOfSale.Services.Models;

namespace PointOfSale.Services
{
    public interface ICustomerService
    {
        Collection<int> GetCustomerIds();
        Customer GetCustomer(int customerId);
    }
}
