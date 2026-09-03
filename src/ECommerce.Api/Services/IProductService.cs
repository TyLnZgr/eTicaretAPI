using ECommerce.Api.Models;

namespace ECommerce.Api.Services;

public interface IProductService
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(int id);
    Product Create(string name, decimal price, int stockQuantity, bool isActive);
    Product? Update(
        int id,
        string name,
        decimal price,
        int stockQuantity,
        bool isActive);
    bool Delete(int id);
}
