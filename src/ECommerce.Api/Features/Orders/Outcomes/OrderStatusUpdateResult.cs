using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Orders.Outcomes;

public sealed record OrderStatusUpdateResult(
    OrderStatusUpdateStatus Status,
    Order? Order = null,
    OrderStatus? CurrentStatus = null,
    int? ProductId = null);
