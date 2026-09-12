namespace ECommerce.Api.Features.Products.Dtos;

public sealed class AdjustProductStockRequest
{
    public int QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
}
