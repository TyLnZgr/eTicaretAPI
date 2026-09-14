using ECommerce.Api.Data;
using ECommerce.Api.Features.Carts.Outcomes;
using ECommerce.Api.Features.Carts.Validation;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Carts.Services;

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
            quantity > CartRequestValidator.MaximumQuantityPerItem)
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
            cart = new Cart
            {
                CustomerId = customerId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.Carts.Add(cart);
        }

        var item = cart.Items.SingleOrDefault(candidate =>
            candidate.ProductId == productId);

        if (item is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                Quantity = quantity
            });
        }
        else if (item.Quantity != quantity)
        {
            item.Quantity = quantity;
        }
        else
        {
            var unchangedCart = await GetAsync(
                customerId,
                cancellationToken);

            return new CartMutationResult(
                CartMutationStatus.Success,
                unchangedCart);
        }

        cart.UpdatedAtUtc = now;

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

        var item = cart?.Items.SingleOrDefault(candidate =>
            candidate.ProductId == productId);

        if (cart is null || item is null)
        {
            return CartItemRemovalStatus.ItemNotFound;
        }

        cart.Items.Remove(item);
        cart.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

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

        if (cart is null || cart.Items.Count == 0)
        {
            return;
        }

        cart.Items.Clear();
        cart.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

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
