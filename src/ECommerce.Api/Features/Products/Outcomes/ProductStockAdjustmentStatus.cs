namespace ECommerce.Api.Features.Products.Outcomes;

public enum ProductStockAdjustmentStatus
{
    Success,
    ProductNotFound,
    InvalidQuantityDelta,
    InvalidReason,
    InsufficientStock,
    StockLimitExceeded
}
