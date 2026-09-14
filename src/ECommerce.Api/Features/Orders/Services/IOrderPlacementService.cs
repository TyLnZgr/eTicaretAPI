using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Features.Orders.Outcomes;

namespace ECommerce.Api.Features.Orders.Services;

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
