using ECommerce.Api.Data;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

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
    public async Task<Product> CreateAsync(
    string name,
    decimal price,
    int stockQuantity,
    bool isActive,
    CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            IsActive = isActive
        };

        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return product;
    }
    public async Task<Product?> UpdateAsync(
    int id,
    string name,
    decimal price,
    int stockQuantity,
    bool isActive,
    CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.IsActive = isActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return product;
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