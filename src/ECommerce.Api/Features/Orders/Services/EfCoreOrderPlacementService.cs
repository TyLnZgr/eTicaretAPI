using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Application.Orders.Services;
using ECommerce.Api.Data;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Orders.Services;

public sealed class EfCoreOrderPlacementService : IOrderPlacementService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreOrderPlacementService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<OrderCreationResult> CreateAsync(
        Guid customerId,
        int addressId,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var result = await CreateCoreAsync(
            customerId,
            addressId,
            items,
            cancellationToken);

        if (result.Status != OrderCreationStatus.Success)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<OrderCreationResult> CheckoutCartAsync(
        Guid customerId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty || addressId <= 0)
        {
            return new OrderCreationResult(
                OrderCreationStatus.InvalidRequest);
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var cart = await _dbContext.Carts
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.CustomerId == customerId,
                cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new OrderCreationResult(
                OrderCreationStatus.CartEmpty);
        }

        var requestedItems = cart.Items
            .Select(item => new CreateOrderItemRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })
            .ToArray();

        var result = await CreateCoreAsync(
            customerId,
            addressId,
            requestedItems,
            cancellationToken);

        if (result.Status != OrderCreationStatus.Success)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        var deletedCarts = await _dbContext.Carts
            .Where(candidate => candidate.Id == cart.Id)
            .Where(candidate => candidate.CustomerId == customerId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCarts == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new OrderCreationResult(
                OrderCreationStatus.ConcurrencyConflict);
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<OrderCreationResult> CreateCoreAsync(
        Guid customerId,
        int addressId,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty ||
            addressId <= 0 ||
            items is null ||
            items.Count == 0 ||
            items.Count > 100 ||
            items.Any(item =>
                item.ProductId <= 0 ||
                item.Quantity <= 0 ||
                item.Quantity > 1_000) ||
            items.Select(item => item.ProductId).Distinct().Count() != items.Count)
        {
            return new OrderCreationResult(
                OrderCreationStatus.InvalidRequest);
        }

        var requestedItems = items
            .OrderBy(item => item.ProductId)
            .ToArray();

        var productIds = requestedItems
            .Select(item => item.ProductId)
            .ToArray();

        var customerEmail = await _dbContext.Users
            .AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => customer.Email)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            return new OrderCreationResult(
                OrderCreationStatus.CustomerNotFound);
        }

        var shippingAddress = await _dbContext.CustomerAddresses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                address =>
                    address.Id == addressId &&
                    address.CustomerId == customerId,
                cancellationToken);

        if (shippingAddress is null)
        {
            return new OrderCreationResult(
                OrderCreationStatus.ShippingAddressNotFound,
                AddressId: addressId);
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(
                product => product.Id,
                cancellationToken);

        foreach (var requestedItem in requestedItems)
        {
            if (!products.TryGetValue(
                    requestedItem.ProductId,
                    out var product))
            {
                return new OrderCreationResult(
                    OrderCreationStatus.ProductNotFound,
                    ProductId: requestedItem.ProductId);
            }

            if (!product.IsActive)
            {
                return new OrderCreationResult(
                    OrderCreationStatus.ProductInactive,
                    ProductId: product.Id);
            }

            if (product.StockQuantity < requestedItem.Quantity)
            {
                return new OrderCreationResult(
                    OrderCreationStatus.InsufficientStock,
                    ProductId: product.Id);
            }
        }

        var order = new Order
        {
            CustomerId = customerId,
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        order.SetShippingAddress(
            new OrderAddressSnapshot(
                shippingAddress.RecipientFullName,
                shippingAddress.PhoneNumber,
                shippingAddress.AddressLine1,
                shippingAddress.AddressLine2,
                shippingAddress.District,
                shippingAddress.City,
                shippingAddress.PostalCode,
                shippingAddress.CountryCode));

        foreach (var requestedItem in requestedItems)
        {
            var product = products[requestedItem.ProductId];
            var lineTotal = product.Price * requestedItem.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = requestedItem.Quantity,
                LineTotal = lineTotal
            });

            order.TotalAmount += lineTotal;
        }

        var stockMovements = new List<StockMovement>();

        foreach (var requestedItem in requestedItems)
        {
            var affectedRows = await _dbContext.Products
                .Where(product => product.Id == requestedItem.ProductId)
                .Where(product => product.IsActive)
                .Where(product =>
                    product.StockQuantity >= requestedItem.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        product => product.StockQuantity,
                        product =>
                            product.StockQuantity - requestedItem.Quantity),
                    cancellationToken);

            if (affectedRows == 0)
            {
                var currentProduct = await _dbContext.Products
                    .AsNoTracking()
                    .Where(product =>
                        product.Id == requestedItem.ProductId)
                    .Select(product => new
                    {
                        product.IsActive,
                        product.StockQuantity
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                if (currentProduct is null)
                {
                    return new OrderCreationResult(
                        OrderCreationStatus.ProductNotFound,
                        ProductId: requestedItem.ProductId);
                }

                return new OrderCreationResult(
                    currentProduct.IsActive
                        ? OrderCreationStatus.InsufficientStock
                        : OrderCreationStatus.ProductInactive,
                    ProductId: requestedItem.ProductId);
            }

            var stockQuantityAfter = await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id == requestedItem.ProductId)
                .Select(product => product.StockQuantity)
                .SingleAsync(cancellationToken);

            stockMovements.Add(new StockMovement
            {
                ProductId = requestedItem.ProductId,
                QuantityDelta = -requestedItem.Quantity,
                StockQuantityAfter = stockQuantityAfter,
                Reason = "Order placement",
                CreatedAtUtc = order.CreatedAtUtc
            });
        }

        _dbContext.Orders.Add(order);
        _dbContext.StockMovements.AddRange(stockMovements);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OrderCreationResult(
            OrderCreationStatus.Success,
            order);
    }
}
