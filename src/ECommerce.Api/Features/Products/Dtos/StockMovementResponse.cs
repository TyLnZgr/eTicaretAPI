namespace ECommerce.Api.Features.Products.Dtos;

public sealed record StockMovementResponse(
    int Id,
    int ProductId,
    int QuantityDelta,
    int StockQuantityAfter,
    string Reason,
    DateTime CreatedAtUtc);
