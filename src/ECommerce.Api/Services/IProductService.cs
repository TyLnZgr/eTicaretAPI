using ECommerce.Api.Models;

namespace ECommerce.Api.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Product> CreateAsync(
        string name,
        decimal price,
        int stockQuantity,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<Product?> UpdateAsync(
        int id,
        string name,
        decimal price,
        int stockQuantity,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}