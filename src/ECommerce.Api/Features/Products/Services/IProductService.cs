using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Outcomes;

namespace ECommerce.Api.Features.Products.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetAllAsync(
        ProductQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<ProductResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementResponse>?> GetStockMovementsAsync(
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
        int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<ProductStockAdjustmentStatus> AdjustStockAsync(
        int id,
        int quantityDelta,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}
