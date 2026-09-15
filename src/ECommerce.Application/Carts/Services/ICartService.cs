using ECommerce.Application.Carts.Outcomes;
using ECommerce.Domain.Carts;

namespace ECommerce.Application.Carts.Services;

public interface ICartService
{
    Task<Cart?> GetAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CartMutationResult> SetItemQuantityAsync(
        Guid customerId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<CartItemRemovalStatus> RemoveItemAsync(
        Guid customerId,
        int productId,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
