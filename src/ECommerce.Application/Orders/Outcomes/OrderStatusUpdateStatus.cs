namespace ECommerce.Application.Orders.Outcomes;

public enum OrderStatusUpdateStatus
{
    Success,
    OrderNotFound,
    InvalidTransition,
    ConcurrencyConflict,
    StockLimitExceeded
}
