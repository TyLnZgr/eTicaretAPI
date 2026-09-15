namespace ECommerce.Application.Carts.Outcomes;

public enum CartMutationStatus
{
    Success,
    InvalidRequest,
    CustomerNotFound,
    ProductNotFound,
    ProductInactive,
    InsufficientStock
}
