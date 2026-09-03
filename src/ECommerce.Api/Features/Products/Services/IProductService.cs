using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Outcomes;

namespace ECommerce.Api.Features.Products.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(
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
