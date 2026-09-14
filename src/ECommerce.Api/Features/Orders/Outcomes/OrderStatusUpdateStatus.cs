namespace ECommerce.Api.Features.Orders.Outcomes;

public enum OrderStatusUpdateStatus
{
    Success,
    OrderNotFound,
    InvalidTransition,
    ConcurrencyConflict,
    StockLimitExceeded
}
