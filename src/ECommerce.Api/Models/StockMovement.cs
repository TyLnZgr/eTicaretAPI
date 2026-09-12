namespace ECommerce.Api.Models;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityDelta { get; set; }
    public int StockQuantityAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public Product Product { get; set; } = null!;
}
