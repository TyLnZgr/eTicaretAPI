using ECommerce.Api.Models;
using ECommerce.Api.Services.Results;

namespace ECommerce.Api.Services;

public class InMemoryProductService : IProductService
{
    private readonly List<Product> _products;
    private readonly HashSet<int> _categoryIds = new() { 1 };
    public InMemoryProductService()
    {
        _products = new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Mechanical Keyboard",
                Price = 2499.90m,
                StockQuantity = 25,
                IsActive = true
            },
            new Product
            {
                Id = 2,
                Name = "Wireless Mouse",
                Price = 1299.50m,
                StockQuantity = 40,
                IsActive = true
            },
            new Product
            {
                Id = 3,
                Name = "4K Monitor",
                Price = 12999.00m,
                StockQuantity = 0,
                IsActive = false
            }
        };
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Product> products = _products.ToArray();

        return Task.FromResult(products);
    }

    public Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = FindById(id);

        return Task.FromResult(product);
    }

    public Task<ProductMutationResult> CreateAsync(
    string name,
    decimal price,
    int stockQuantity,
    int categoryId,
    bool isActive,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_categoryIds.Contains(categoryId))
        {
            return Task.FromResult(
                new ProductMutationResult(
                    ProductMutationStatus.CategoryNotFound));
        }

        var nextId = 1;

        if (_products.Count > 0)
        {
            nextId = _products.Max(candidate => candidate.Id) + 1;
        }

        var product = new Product
        {
            Id = nextId,
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            CategoryId = categoryId,
            IsActive = isActive
        };

        _products.Add(product);

        return Task.FromResult(
            new ProductMutationResult(
                ProductMutationStatus.Success,
                product));
    }

    public Task<ProductMutationResult> UpdateAsync(
    int id,
    string name,
    decimal price,
    int stockQuantity,
    int categoryId,
    bool isActive,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = FindById(id);

        if (product is null)
        {
            return Task.FromResult(
                new ProductMutationResult(
                    ProductMutationStatus.ProductNotFound));
        }

        if (!_categoryIds.Contains(categoryId))
        {
            return Task.FromResult(
                new ProductMutationResult(
                    ProductMutationStatus.CategoryNotFound));
        }

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.CategoryId = categoryId;
        product.IsActive = isActive;

        return Task.FromResult(
            new ProductMutationResult(
                ProductMutationStatus.Success,
                product));
    }

    public Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = FindById(id);

        if (product is null)
        {
            return Task.FromResult(false);
        }

        var wasDeleted = _products.Remove(product);

        return Task.FromResult(wasDeleted);
    }

    private Product? FindById(int id)
    {
        return _products.FirstOrDefault(candidate => candidate.Id == id);
    }
}
