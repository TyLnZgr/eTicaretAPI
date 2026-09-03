using ECommerce.Api.Models;

namespace ECommerce.Api.Services;

public class InMemoryProductService : IProductService
{
    private readonly List<Product> _products;

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

    public Task<Product> CreateAsync(
        string name,
        decimal price,
        int stockQuantity,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            IsActive = isActive
        };

        _products.Add(product);

        return Task.FromResult(product);
    }

    public Task<Product?> UpdateAsync(
        int id,
        string name,
        decimal price,
        int stockQuantity,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = FindById(id);

        if (product is null)
        {
            return Task.FromResult<Product?>(null);
        }

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.IsActive = isActive;

        return Task.FromResult<Product?>(product);
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
