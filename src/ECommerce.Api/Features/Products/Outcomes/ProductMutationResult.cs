using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Products.Outcomes;

public enum ProductMutationStatus
{
    Success,
    ProductNotFound,
    CategoryNotFound
}

public sealed record ProductMutationResult(
    ProductMutationStatus Status,
    Product? Product = null);
