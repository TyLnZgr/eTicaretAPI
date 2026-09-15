using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Outcomes;

namespace ECommerce.Application.Orders.Services;

public interface IOrderPlacementService
{
    Task<OrderCreationResult> CreateAsync(
        Guid customerId,
        int addressId,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken = default);

    Task<OrderCreationResult> CheckoutCartAsync(
        Guid customerId,
        int addressId,
        CancellationToken cancellationToken = default);
}
