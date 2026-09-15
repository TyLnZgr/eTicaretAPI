using ECommerce.Domain.Orders;

namespace ECommerce.Api.Features.Orders.Outcomes;

public sealed record OrderCreationResult(
    OrderCreationStatus Status,
    Order? Order = null,
    int? ProductId = null,
    int? AddressId = null);
