namespace ECommerce.Domain.Catalog;

public enum ProductStockChangeStatus
{
    Success,
    InvalidQuantityDelta,
    InvalidReason,
    InsufficientStock,
    StockLimitExceeded
}
