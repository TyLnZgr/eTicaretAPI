using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Carts.Outcomes;

public sealed record CartMutationResult(
    CartMutationStatus Status,
    Cart? Cart = null,
    int? AvailableStock = null);
