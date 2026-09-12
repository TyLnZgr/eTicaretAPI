namespace ECommerce.Api.Features.Products.Outcomes;

public enum ProductStockAdjustmentStatus
{
    Success,
    ProductNotFound,
    InvalidQuantityDelta,
    InsufficientStock,
    StockLimitExceeded
}
