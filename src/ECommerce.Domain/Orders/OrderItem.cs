using ECommerce.Domain.Catalog;

namespace ECommerce.Domain.Orders;

public class OrderItem
{
    public const int MaxProductNameLength = 200;
    public const int MaxQuantity = 1_000;

    internal OrderItem()
    {
    }

    internal OrderItem(
        int productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId),
                "Product ID must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException(
                "A product name is required.",
                nameof(productName));
        }

        var normalizedProductName = productName.Trim();

        if (normalizedProductName.Length > MaxProductNameLength)
        {
            throw new ArgumentException(
                $"Product name cannot exceed " +
                $"{MaxProductNameLength} characters.",
                nameof(productName));
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price must be greater than zero.");
        }

        if (quantity is < 1 or > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Quantity must be between 1 and {MaxQuantity}.");
        }

        ProductId = productId;
        ProductName = normalizedProductName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = checked(unitPrice * quantity);
    }

    public int Id { get; set; }
    public int OrderId { get; internal set; }
    public int? ProductId { get; internal set; }
    public string ProductName { get; internal set; } = string.Empty;
    public decimal UnitPrice { get; internal set; }
    public int Quantity { get; internal set; }
    public decimal LineTotal { get; internal set; }

    public Order Order { get; internal set; } = null!;
    public Product? Product { get; internal set; }
}
