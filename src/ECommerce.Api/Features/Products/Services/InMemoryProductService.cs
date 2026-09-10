using ECommerce.Api.Common.Pagination;
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

    public Task<PagedResult<ProductResponse>> GetAllAsync(
        ProductQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<Product> query = _products;

        var searchTerm = string.IsNullOrWhiteSpace(queryParameters.Search)
            ? null
            : queryParameters.Search.Trim();

        if (searchTerm is not null)
        {
            query = query.Where(product =>
                product.Name.Contains(
                    searchTerm,
                    StringComparison.Ordinal));
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

        var totalCount = query.Count();

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
                .OrderBy(product => product.Name, StringComparer.Ordinal)
                .ThenBy(product => product.Id),

            ("name", "desc") => query
                .OrderByDescending(product => product.Name, StringComparer.Ordinal)
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

        query = query
            .Skip(skip)
            .Take(pageSize);

        IReadOnlyList<ProductResponse> items = query
            .Select(ToResponse)
            .ToArray();

        var result = new PagedResult<ProductResponse>(
            items,
            page,
            pageSize,
            totalCount);

        return Task.FromResult(result);
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
