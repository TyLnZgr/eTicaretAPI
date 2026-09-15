namespace ECommerce.Domain.Catalog;

public sealed record ProductStockChangeResult(
    ProductStockChangeStatus Status,
    StockMovement? Movement = null);
