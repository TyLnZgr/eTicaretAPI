using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using ECommerce.Api.Services.Results;

namespace ECommerce.Api.Services;

public class EfCoreProductService : IProductService
{
    private readonly ECommerceDbContext _dbContext;

    public EfCoreProductService(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);
    }
    public async Task<ProductMutationResult> CreateAsync(
        string name,
        decimal price,
        int stockQuantity,
        int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Id == categoryId,
                cancellationToken);

        if (!categoryExists)
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

        var categoryExists = await _dbContext.Categories
            .AnyAsync(
                category => category.Id == categoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return new ProductMutationResult(
                ProductMutationStatus.CategoryNotFound);
        }

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.CategoryId = categoryId;
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