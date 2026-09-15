namespace ECommerce.Domain.Catalog;

public class StockMovement
{
    public const int MaxReasonLength = 200;

    private StockMovement()
    {
    }

    public StockMovement(
        Product product,
        int quantityDelta,
        int stockQuantityAfter,
        string reason,
        DateTime createdAtUtc)
        : this(
            product?.Id ?? throw new ArgumentNullException(nameof(product)),
            quantityDelta,
            stockQuantityAfter,
            reason,
            createdAtUtc)
    {
        Product = product;
    }

    public StockMovement(
        int productId,
        int quantityDelta,
        int stockQuantityAfter,
        string reason,
        DateTime createdAtUtc)
    {
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityDelta),
                "Quantity delta must be different from zero.");
        }

        if (stockQuantityAfter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantityAfter),
                "Stock quantity after the movement cannot be negative.");
        }

        if (!IsReasonValid(reason))
        {
            throw new ArgumentException(
                $"Reason must be between 1 and {MaxReasonLength} characters.",
                nameof(reason));
        }

        ProductId = productId;
        QuantityDelta = quantityDelta;
        StockQuantityAfter = stockQuantityAfter;
        Reason = reason.Trim();
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
    }

    public int Id { get; set; }
    public int ProductId { get; private set; }
    public int QuantityDelta { get; private set; }
    public int StockQuantityAfter { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public Product Product { get; private set; } = null!;

    internal static bool IsReasonValid(string reason)
    {
        return !string.IsNullOrWhiteSpace(reason) &&
            reason.Trim().Length <= MaxReasonLength;
    }
}
