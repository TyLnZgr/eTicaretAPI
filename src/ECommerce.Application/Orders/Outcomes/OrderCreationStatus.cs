namespace ECommerce.Application.Orders.Outcomes;

public enum OrderCreationStatus
{
    Success,
    InvalidRequest,
    CustomerNotFound,
    ShippingAddressNotFound,
    ProductNotFound,
    ProductInactive,
    InsufficientStock,
    CartEmpty,
    ConcurrencyConflict
}
