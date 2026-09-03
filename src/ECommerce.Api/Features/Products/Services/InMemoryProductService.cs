using ECommerce.Api.Features.Products.Dtos;
using ECommerce.Api.Features.Products.Outcomes;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Products.Services;

public class InMemoryProductService : IProductService
{
    private readonly List<Product> _products;
    private readonly Dictionary<int, Category> _categories = new()
    {
        [1] = new Category
        {
            Id = 1,
            Name = "Uncategorized",
            IsActive = true
        }
    };

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
                CategoryId = 1,
                Category = _categories[1],
                IsActive = true
            },
            new Product
            {
                Id = 2,
                Name = "Wireless Mouse",
                Price = 1299.50m,
                StockQuantity = 40,
                CategoryId = 1,
                Category = _categories[1],
                IsActive = true
            },
            new Product
            {
                Id = 3,
                Name = "4K Monitor",
                Price = 12999.00m,
                StockQuantity = 0,
                CategoryId = 1,
                Category = _categories[1],
                IsActive = false
            }
        };
    }

    public Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ProductResponse> products = _products
            .Select(ToResponse)
            .ToArray();

        return Task.FromResult(products);
    }

    public Task<ProductResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var product = FindById(id);
        var response = product is null
            ? null
            : ToResponse(product);

        return Task.FromResult(response);
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

        if (!_categories.TryGetValue(categoryId, out var category))
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
            Category = category,
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

        if (!_categories.TryGetValue(categoryId, out var category))
        {
            return Task.FromResult(
                new ProductMutationResult(
                    ProductMutationStatus.CategoryNotFound));
        }

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.CategoryId = categoryId;
        product.Category = category;
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

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Price,
            product.StockQuantity,
            product.IsActive,
            product.CategoryId,
            product.Category.Name);
    }
}
