namespace ECommerce.Api.Features.Carts.Outcomes;

public enum CartMutationStatus
{
    Success,
    InvalidRequest,
    CustomerNotFound,
    ProductNotFound,
    ProductInactive,
    InsufficientStock
}
