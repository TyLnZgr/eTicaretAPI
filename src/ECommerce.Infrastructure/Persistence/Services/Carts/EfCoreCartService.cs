using ECommerce.Application.Carts.Outcomes;
using ECommerce.Application.Carts.Services;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Domain.Carts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Services.Carts;

public sealed class EfCoreCartService : ICartService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreCartService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Cart?> GetAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await QueryCart(customerId)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CartMutationResult> SetItemQuantityAsync(
        Guid customerId,
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty ||
            productId <= 0 ||
            quantity < 1 ||
            quantity > CartItem.MaximumQuantity)
        {
            return new CartMutationResult(
                CartMutationStatus.InvalidRequest);
        }

        var customerExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                customer => customer.Id == customerId,
                cancellationToken);

        if (!customerExists)
        {
            return new CartMutationResult(
                CartMutationStatus.CustomerNotFound);
        }

        var product = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == productId,
                cancellationToken);

        if (product is null)
        {
            return new CartMutationResult(
                CartMutationStatus.ProductNotFound);
        }

        if (!product.IsActive)
        {
            return new CartMutationResult(
                CartMutationStatus.ProductInactive,
                AvailableStock: product.StockQuantity);
        }

        if (product.StockQuantity < quantity)
        {
            return new CartMutationResult(
                CartMutationStatus.InsufficientStock,
                AvailableStock: product.StockQuantity);
        }

        var cart = await _dbContext.Carts
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.CustomerId == customerId,
                cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (cart is null)
        {
            cart = new Cart(customerId, now);

            _dbContext.Carts.Add(cart);
        }

        if (!cart.SetItemQuantity(productId, quantity, now))
        {
            var unchangedCart = await GetAsync(
                customerId,
                cancellationToken);

            return new CartMutationResult(
                CartMutationStatus.Success,
                unchangedCart);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedCart = await GetAsync(
            customerId,
            cancellationToken);

        return new CartMutationResult(
            CartMutationStatus.Success,
            updatedCart);
    }

    public async Task<CartItemRemovalStatus> RemoveItemAsync(
        Guid customerId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _dbContext.Carts
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.CustomerId == customerId,
                cancellationToken);

        if (cart is null || !cart.RemoveItem(
                productId,
                _timeProvider.GetUtcNow().UtcDateTime))
        {
            return CartItemRemovalStatus.ItemNotFound;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CartItemRemovalStatus.Success;
    }

    public async Task ClearAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _dbContext.Carts
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.CustomerId == customerId,
                cancellationToken);

        if (cart is null || !cart.Clear(
                _timeProvider.GetUtcNow().UtcDateTime))
        {
            return;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Cart> QueryCart(Guid customerId)
    {
        return _dbContext.Carts
            .Where(cart => cart.CustomerId == customerId)
            .Include(cart => cart.Items)
            .ThenInclude(item => item.Product);
    }
}
