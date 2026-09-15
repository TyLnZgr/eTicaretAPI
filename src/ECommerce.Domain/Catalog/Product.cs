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
        bool isActive,
        DateTime createdAtUtc)
    {
        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity),
                "Stock quantity cannot be negative.");
        }

        CreatedAtUtc = NormalizeUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 0;
        UpdateDetails(
            name,
            price,
            category,
            isActive,
            CreatedAtUtc);
        StockQuantity = stockQuantity;
    }

    public int Id { get; set; }
    public string Name { get; internal set; } = string.Empty;
    public decimal Price { get; internal set; }
    public int StockQuantity { get; internal set; }
    public bool IsActive { get; internal set; }
    public int CategoryId { get; internal set; }
    public long Version { get; private set; } = 1;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UnixEpoch;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UnixEpoch;
    public DateTime? DeletedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }

    public Category Category { get; internal set; } = null!;
    public IReadOnlyCollection<StockMovement> StockMovements =>
        _stockMovements;
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems;
    public IReadOnlyCollection<CartItem> CartItems => _cartItems;

    public void UpdateDetails(
        string name,
        decimal price,
        Category category,
        bool isActive,
        DateTime updatedAtUtc)
    {
        EnsureNotDeleted();

        var normalizedName = NormalizeName(name);

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(category);

        var normalizedUpdatedAtUtc = NormalizeMutationTime(
            updatedAtUtc,
            nameof(updatedAtUtc));
        var nextVersion = checked(Version + 1);

        Name = normalizedName;
        Price = price;
        Category = category;
        CategoryId = category.Id;
        IsActive = isActive;
        UpdatedAtUtc = normalizedUpdatedAtUtc;
        Version = nextVersion;
    }

    public StockMovement? RecordInitialStock(DateTime createdAtUtc)
    {
        EnsureNotDeleted();

        if (StockQuantity == 0)
        {
            return null;
        }

        if (_stockMovements.Count > 0)
        {
            throw new InvalidOperationException(
                "Initial stock has already been recorded.");
        }

        var normalizedCreatedAtUtc = NormalizeMutationTime(
            createdAtUtc,
            nameof(createdAtUtc));

        var movement = new StockMovement(
            this,
            StockQuantity,
            StockQuantity,
            "Initial stock",
            normalizedCreatedAtUtc);

        _stockMovements.Add(movement);
        return movement;
    }

    public ProductStockChangeResult AdjustStock(
        int quantityDelta,
        string reason,
        DateTime createdAtUtc)
    {
        EnsureNotDeleted();

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

        var normalizedCreatedAtUtc = NormalizeMutationTime(
            createdAtUtc,
            nameof(createdAtUtc));
        var nextVersion = checked(Version + 1);
        StockQuantity = (int)requestedStock;

        var movement = new StockMovement(
            this,
            quantityDelta,
            StockQuantity,
            reason,
            normalizedCreatedAtUtc);

        _stockMovements.Add(movement);
        UpdatedAtUtc = normalizedCreatedAtUtc;
        Version = nextVersion;

        return new ProductStockChangeResult(
            ProductStockChangeStatus.Success,
            movement);
    }

    public bool MarkAsDeleted(DateTime deletedAtUtc)
    {
        if (IsDeleted)
        {
            return false;
        }

        var normalizedDeletedAtUtc = NormalizeMutationTime(
            deletedAtUtc,
            nameof(deletedAtUtc));
        var nextVersion = checked(Version + 1);

        IsDeleted = true;
        IsActive = false;
        DeletedAtUtc = normalizedDeletedAtUtc;
        UpdatedAtUtc = normalizedDeletedAtUtc;
        Version = nextVersion;

        return true;
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

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException(
                "A deleted product cannot be modified.");
        }
    }

    private DateTime NormalizeMutationTime(
        DateTime value,
        string parameterName)
    {
        var normalizedValue = NormalizeUtc(value);

        if (normalizedValue < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "The mutation time cannot be earlier than the last update time.");
        }

        return normalizedValue;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
