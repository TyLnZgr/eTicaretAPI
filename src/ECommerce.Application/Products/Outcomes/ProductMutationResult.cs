using ECommerce.Domain.Catalog;

namespace ECommerce.Application.Products.Outcomes;

public enum ProductMutationStatus
{
    Success,
    ProductNotFound,
    CategoryNotFound,
    ConcurrencyConflict
}

public sealed record ProductMutationResult(
    ProductMutationStatus Status,
    Product? Product = null);
