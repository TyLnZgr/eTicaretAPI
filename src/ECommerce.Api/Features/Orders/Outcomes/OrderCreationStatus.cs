namespace ECommerce.Api.Features.Orders.Outcomes;

public enum OrderCreationStatus
{
    Success,
    InvalidRequest,
    ProductNotFound,
    ProductInactive,
    InsufficientStock
}
