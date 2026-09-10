using ECommerce.Api.Data;
using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Outcomes;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Products.Services;

public class EfCoreProductService : IProductService
{
    private readonly ECommerceDbContext _dbContext;

    public EfCoreProductService(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
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
        return await query
            .OrderBy(product => product.Id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity,
                product.IsActive,
                product.CategoryId,
                product.Category.Name))
            .ToListAsync(cancellationToken);
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

        var product = new Product
        {
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            CategoryId = categoryId,
            Category = category,
            IsActive = isActive
        };

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
        int stockQuantity,
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

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.CategoryId = categoryId;
        product.Category = category;
        product.IsActive = isActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductMutationResult(
            ProductMutationStatus.Success,
            product);
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
