namespace ECommerce.Api.Features.Orders.Outcomes;

public enum OrderCreationStatus
{
    Success,
    InvalidRequest,
    CustomerNotFound,
    ProductNotFound,
    ProductInactive,
    InsufficientStock
}
