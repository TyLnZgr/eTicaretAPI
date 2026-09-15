using ECommerce.Domain.Carts;
using ECommerce.Domain.Orders;

namespace ECommerce.Domain.Catalog;

public class Product
{
    public const int MaxNameLength = 200;

    private readonly List<StockMovement> _stockMovements = [];
    private readonly List<OrderItem> _orderItems = [];
    private readonly List<CartItem> _cartItems = [];

    internal Product()
    {
    }

    public Product(
        string name,
        decimal price,
        int stockQuantity,
        Category category,
        bool isActive)
    {
        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity),
                "Stock quantity cannot be negative.");
        }

        UpdateDetails(name, price, category, isActive);
        StockQuantity = stockQuantity;
    }

    public int Id { get; set; }
    public string Name { get; internal set; } = string.Empty;
    public decimal Price { get; internal set; }
    public int StockQuantity { get; internal set; }
    public bool IsActive { get; internal set; }
    public int CategoryId { get; internal set; }

    public Category Category { get; internal set; } = null!;
    public IReadOnlyCollection<StockMovement> StockMovements =>
        _stockMovements;
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems;
    public IReadOnlyCollection<CartItem> CartItems => _cartItems;

    public void UpdateDetails(
        string name,
        decimal price,
        Category category,
        bool isActive)
    {
        Name = NormalizeName(name);

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(category);

        Price = price;
        Category = category;
        CategoryId = category.Id;
        IsActive = isActive;
    }

    public StockMovement? RecordInitialStock(DateTime createdAtUtc)
    {
        if (StockQuantity == 0)
        {
            return null;
        }

        if (_stockMovements.Count > 0)
        {
            throw new InvalidOperationException(
                "Initial stock has already been recorded.");
        }

        var movement = new StockMovement(
            this,
            StockQuantity,
            StockQuantity,
            "Initial stock",
            createdAtUtc);

        _stockMovements.Add(movement);
        return movement;
    }

    public ProductStockChangeResult AdjustStock(
        int quantityDelta,
        string reason,
        DateTime createdAtUtc)
    {
        if (quantityDelta == 0)
        {
            return new ProductStockChangeResult(
                ProductStockChangeStatus.InvalidQuantityDelta);
        }

        if (!StockMovement.IsReasonValid(reason))
        {
            return new ProductStockChangeResult(
                ProductStockChangeStatus.InvalidReason);
        }

        var requestedStock = (long)StockQuantity + quantityDelta;

        if (requestedStock < 0)
        {
            return new ProductStockChangeResult(
                ProductStockChangeStatus.InsufficientStock);
        }

        if (requestedStock > int.MaxValue)
        {
            return new ProductStockChangeResult(
                ProductStockChangeStatus.StockLimitExceeded);
        }

        StockQuantity = (int)requestedStock;

        var movement = new StockMovement(
            this,
            quantityDelta,
            StockQuantity,
            reason,
            createdAtUtc);

        _stockMovements.Add(movement);

        return new ProductStockChangeResult(
            ProductStockChangeStatus.Success,
            movement);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Product name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        return normalizedName;
    }
}
