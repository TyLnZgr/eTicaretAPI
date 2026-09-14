using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Features.Orders.Outcomes;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Orders.Services;

public interface IOrderService
{
    Task<PagedResult<OrderResponse>> GetAllAsync(
        OrderQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OrderCreationResult> CreateAsync(
        string customerEmail,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken = default);

    Task<OrderStatusUpdateResult> UpdateStatusAsync(
        int id,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default);
}
