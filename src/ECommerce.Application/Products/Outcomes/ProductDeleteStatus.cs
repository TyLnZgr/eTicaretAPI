namespace ECommerce.Application.Products.Outcomes;

public enum ProductDeleteStatus
{
    Success,
    ProductNotFound,
    ConcurrencyConflict
}
