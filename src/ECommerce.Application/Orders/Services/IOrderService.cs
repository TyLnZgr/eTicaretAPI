using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Domain.Orders;

namespace ECommerce.Application.Orders.Services;

public interface IOrderService
{
    Task<PagedResult<OrderResponse>> GetAllAsync(
        Guid customerId,
        OrderQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AdminOrderResponse>> GetAllAsAdministratorAsync(
        AdminOrderQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        int id,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<OrderStatusUpdateResult> UpdateStatusAsync(
        int id,
        Guid customerId,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default);

    Task<OrderStatusUpdateResult> UpdateStatusAsAdministratorAsync(
        int id,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default);
}
