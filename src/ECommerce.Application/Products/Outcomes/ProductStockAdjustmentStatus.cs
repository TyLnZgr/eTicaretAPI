namespace ECommerce.Application.Products.Outcomes;

public enum ProductStockAdjustmentStatus
{
    Success,
    ProductNotFound,
    InvalidQuantityDelta,
    InvalidReason,
    InsufficientStock,
    StockLimitExceeded
}
