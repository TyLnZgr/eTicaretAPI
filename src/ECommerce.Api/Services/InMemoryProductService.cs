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

    public IReadOnlyList<Product> GetAll()
    {
        return _products.ToArray();
    }

    public Product? GetById(int id)
    {
        return _products.FirstOrDefault(candidate => candidate.Id == id);
    }

    public Product Create(
        string name,
        decimal price,
        int stockQuantity,
        bool isActive)
    {
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

        return product;
    }

    public Product? Update(
        int id,
        string name,
        decimal price,
        int stockQuantity,
        bool isActive)
    {
        var product = GetById(id);

        if (product is null)
        {
            return null;
        }

        product.Name = name;
        product.Price = price;
        product.StockQuantity = stockQuantity;
        product.IsActive = isActive;

        return product;
    }

    public bool Delete(int id)
    {
        var product = GetById(id);

        if (product is null)
        {
            return false;
        }

        return _products.Remove(product);
    }
}
