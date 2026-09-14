using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Orders.Outcomes;

public sealed record OrderCreationResult(
    OrderCreationStatus Status,
    Order? Order = null,
    int? ProductId = null);
