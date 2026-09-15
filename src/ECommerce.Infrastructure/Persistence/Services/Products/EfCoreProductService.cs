using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Outcomes;
using ECommerce.Application.Products.Services;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Services.Products;

public class EfCoreProductService : IProductService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreProductService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<ProductResponse>> GetAllAsync(
        ProductQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking();

        var searchTerm = string.IsNullOrWhiteSpace(queryParameters.Search)
            ? null
            : queryParameters.Search.Trim();

        if (searchTerm is not null)
        {
            query = query.Where(product =>
                product.Name.Contains(searchTerm));
        }

        if (queryParameters.CategoryId.HasValue)
        {
            query = query.Where(product =>
                product.CategoryId == queryParameters.CategoryId.Value);
        }

        if (queryParameters.IsActive.HasValue)
        {
            query = query.Where(product =>
                product.IsActive == queryParameters.IsActive.Value);
        }

        if (queryParameters.MinPrice.HasValue)
        {
            query = query.Where(product =>
                product.Price >= queryParameters.MinPrice.Value);
        }

        if (queryParameters.MaxPrice.HasValue)
        {
            query = query.Where(product =>
                product.Price <= queryParameters.MaxPrice.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var sortBy = string.IsNullOrWhiteSpace(queryParameters.SortBy)
            ? "id"
            : queryParameters.SortBy.Trim().ToLowerInvariant();

        var sortDirection =
            string.IsNullOrWhiteSpace(queryParameters.SortDirection)
                ? "asc"
                : queryParameters.SortDirection.Trim().ToLowerInvariant();

        query = (sortBy, sortDirection) switch
        {
            ("name", "asc") => query
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id),

            ("name", "desc") => query
                .OrderByDescending(product => product.Name)
                .ThenBy(product => product.Id),

            ("stockquantity", "asc") => query
                .OrderBy(product => product.StockQuantity)
                .ThenBy(product => product.Id),

            ("stockquantity", "desc") => query
                .OrderByDescending(product => product.StockQuantity)
                .ThenBy(product => product.Id),

            ("id", "desc") => query
                .OrderByDescending(product => product.Id),

            ("price", "asc") => query
                .OrderBy(product => product.Price)
                .ThenBy(product => product.Id),

            ("price", "desc") => query
                .OrderByDescending(product => product.Price)
                .ThenBy(product => product.Id),

            _ => query.OrderBy(product => product.Id)
        };

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var skip = (page - 1) * pageSize;

        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity,
                product.IsActive,
                product.CategoryId,
                product.Category.Name))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<ProductResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity,
                product.IsActive,
                product.CategoryId,
                product.Category.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementResponse>?> GetStockMovementsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var productExists = await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.Id == id,
                cancellationToken);

        if (!productExists)
        {
            return null;
        }

        return await _dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == id)
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .ThenByDescending(movement => movement.Id)
            .Select(movement => new StockMovementResponse(
                movement.Id,
                movement.ProductId,
                movement.QuantityDelta,
                movement.StockQuantityAfter,
                movement.Reason,
                movement.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductMutationResult> CreateAsync(
        string name,
        decimal price,
        int stockQuantity,
        int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(
                category => category.Id == categoryId,
                cancellationToken);

        if (category is null)
        {
            return new ProductMutationResult(
                ProductMutationStatus.CategoryNotFound);
        }

        var product = new Product(
            name,
            price,
            stockQuantity,
            category,
            isActive);

        product.RecordInitialStock(
            _timeProvider.GetUtcNow().UtcDateTime);

        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductMutationResult(
            ProductMutationStatus.Success,
            product);
    }
    public async Task<ProductMutationResult> UpdateAsync(
        int id,
        string name,
        decimal price,
        int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return new ProductMutationResult(
                ProductMutationStatus.ProductNotFound);
        }

        var category = await _dbContext.Categories
            .SingleOrDefaultAsync(
                category => category.Id == categoryId,
                cancellationToken);

        if (category is null)
        {
            return new ProductMutationResult(
                ProductMutationStatus.CategoryNotFound);
        }

        product.UpdateDetails(name, price, category, isActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductMutationResult(
            ProductMutationStatus.Success,
            product);
    }

    public async Task<ProductStockAdjustmentStatus> AdjustStockAsync(
        int id,
        int quantityDelta,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (quantityDelta == 0)
        {
            return ProductStockAdjustmentStatus.InvalidQuantityDelta;
        }

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Trim().Length > 200)
        {
            return ProductStockAdjustmentStatus.InvalidReason;
        }

        reason = reason.Trim();

        var quantityDeltaAsLong = (long)quantityDelta;

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var affectedRows = await _dbContext.Products
            .Where(product => product.Id == id)
            .Where(product =>
                (long)product.StockQuantity + quantityDeltaAsLong >= 0 &&
                (long)product.StockQuantity + quantityDeltaAsLong <= int.MaxValue)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    product => product.StockQuantity,
                    product => product.StockQuantity + quantityDelta),
                cancellationToken);

        if (affectedRows == 1)
        {
            var stockQuantityAfter = await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == id)
                .Select(product => product.StockQuantity)
                .SingleAsync(cancellationToken);

            _dbContext.StockMovements.Add(new StockMovement(
                id,
                quantityDelta,
                stockQuantityAfter,
                reason,
                _timeProvider.GetUtcNow().UtcDateTime));

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ProductStockAdjustmentStatus.Success;
        }

        var currentStock = await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => (int?)product.StockQuantity)
            .SingleOrDefaultAsync(cancellationToken);

        if (!currentStock.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);

            return ProductStockAdjustmentStatus.ProductNotFound;
        }

        var requestedStock =
            (long)currentStock.Value + quantityDeltaAsLong;

        await transaction.RollbackAsync(cancellationToken);

        return requestedStock < 0
            ? ProductStockAdjustmentStatus.InsufficientStock
            : ProductStockAdjustmentStatus.StockLimitExceeded;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return false;
        }

        _dbContext.Products.Remove(product);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
