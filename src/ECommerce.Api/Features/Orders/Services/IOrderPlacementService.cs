using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Features.Orders.Outcomes;

namespace ECommerce.Api.Features.Orders.Services;

public interface IOrderPlacementService
{
    Task<OrderCreationResult> CreateAsync(
        Guid customerId,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken = default);

    Task<OrderCreationResult> CheckoutCartAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
