using ECommerce.Api.Models;
using ECommerce.Api.Services.Results;

namespace ECommerce.Api.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ProductMutationResult> CreateAsync(
        string name,
        decimal price,
        int stockQuantity,
         int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<ProductMutationResult> UpdateAsync(
        int id,
        string name,
        decimal price,
        int stockQuantity,
         int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}