using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Application.Orders.Services;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Services.Orders;

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
        string idempotencyKey,
        int addressId,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty ||
            !Order.IsIdempotencyKeyValid(idempotencyKey) ||
            !IsOrderRequestValid(addressId, items))
        {
            return new OrderCreationResult(
                OrderCreationStatus.InvalidRequest);
        }

        var normalizedIdempotencyKey = idempotencyKey.Trim();
        var requestFingerprint = CreateRequestFingerprint(
            "items",
            addressId,
            items);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var existingResult = await FindIdempotencyResultAsync(
            customerId,
            normalizedIdempotencyKey,
            requestFingerprint,
            cancellationToken);

        if (existingResult is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return existingResult;
        }

        try
        {
            var result = await CreateCoreAsync(
                customerId,
                normalizedIdempotencyKey,
                requestFingerprint,
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
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            var replayResult = await FindIdempotencyResultAsync(
                customerId,
                normalizedIdempotencyKey,
                requestFingerprint,
                cancellationToken);

            if (replayResult is null)
            {
                throw;
            }

            return replayResult;
        }
    }

    public async Task<OrderCreationResult> CheckoutCartAsync(
        Guid customerId,
        string idempotencyKey,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty ||
            !Order.IsIdempotencyKeyValid(idempotencyKey) ||
            addressId <= 0)
        {
            return new OrderCreationResult(
                OrderCreationStatus.InvalidRequest);
        }

        var normalizedIdempotencyKey = idempotencyKey.Trim();
        var requestFingerprint = CreateRequestFingerprint(
            "cart",
            addressId,
            Array.Empty<CreateOrderItemRequest>());

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var existingResult = await FindIdempotencyResultAsync(
            customerId,
            normalizedIdempotencyKey,
            requestFingerprint,
            cancellationToken);

        if (existingResult is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return existingResult;
        }

        try
        {
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
                normalizedIdempotencyKey,
                requestFingerprint,
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
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            var replayResult = await FindIdempotencyResultAsync(
                customerId,
                normalizedIdempotencyKey,
                requestFingerprint,
                cancellationToken);

            if (replayResult is null)
            {
                throw;
            }

            return replayResult;
        }
    }

    private async Task<OrderCreationResult> CreateCoreAsync(
        Guid customerId,
        string idempotencyKey,
        string requestFingerprint,
        int addressId,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken)
    {
        if (customerId == Guid.Empty ||
            !IsOrderRequestValid(addressId, items))
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

        var order = new Order(
            customerId,
            customerEmail,
            new OrderAddressSnapshot(
                shippingAddress.RecipientFullName,
                shippingAddress.PhoneNumber,
                shippingAddress.AddressLine1,
                shippingAddress.AddressLine2,
                shippingAddress.District,
                shippingAddress.City,
                shippingAddress.PostalCode,
                shippingAddress.CountryCode),
            _timeProvider.GetUtcNow().UtcDateTime,
            idempotencyKey,
            requestFingerprint);

        foreach (var requestedItem in requestedItems)
        {
            var product = products[requestedItem.ProductId];
            order.AddItem(
                product.Id,
                product.Name,
                product.Price,
                requestedItem.Quantity);
        }

        order.EnsureReadyForPlacement();

        var stockMovements = new List<StockMovement>();

        foreach (var requestedItem in requestedItems)
        {
            var affectedRows = await _dbContext.Products
                .Where(product => product.Id == requestedItem.ProductId)
                .Where(product => product.IsActive)
                .Where(product =>
                    product.StockQuantity >= requestedItem.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            product => product.StockQuantity,
                            product =>
                                product.StockQuantity - requestedItem.Quantity)
                        .SetProperty(
                            product => product.Version,
                            product => product.Version + 1)
                        .SetProperty(
                            product => product.UpdatedAtUtc,
                            order.CreatedAtUtc),
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

            stockMovements.Add(new StockMovement(
                requestedItem.ProductId,
                -requestedItem.Quantity,
                stockQuantityAfter,
                "Order placement",
                order.CreatedAtUtc));
        }

        _dbContext.Orders.Add(order);
        _dbContext.StockMovements.AddRange(stockMovements);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OrderCreationResult(
            OrderCreationStatus.Success,
            order);
    }

    private async Task<OrderCreationResult?> FindIdempotencyResultAsync(
        Guid customerId,
        string idempotencyKey,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        var existingOrder = await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(
                order =>
                    order.CustomerId == customerId &&
                    order.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existingOrder is null)
        {
            return null;
        }

        if (existingOrder.RequestFingerprint != requestFingerprint)
        {
            return new OrderCreationResult(
                OrderCreationStatus.IdempotencyConflict);
        }

        return new OrderCreationResult(
            OrderCreationStatus.Success,
            existingOrder,
            WasReplay: true);
    }

    private static bool IsOrderRequestValid(
        int addressId,
        IReadOnlyList<CreateOrderItemRequest>? items)
    {
        return addressId > 0 &&
               items is not null &&
               items.Count > 0 &&
               items.Count <= Order.MaxItemCount &&
               items.All(item =>
                   item.ProductId > 0 &&
                   item.Quantity > 0 &&
                   item.Quantity <= OrderItem.MaxQuantity) &&
               items.Select(item => item.ProductId).Distinct().Count() ==
               items.Count;
    }

    private static string CreateRequestFingerprint(
        string operation,
        int addressId,
        IEnumerable<CreateOrderItemRequest> items)
    {
        var value = new StringBuilder()
            .Append(operation)
            .Append('\n')
            .Append(addressId.ToString(CultureInfo.InvariantCulture));

        foreach (var item in items.OrderBy(item => item.ProductId))
        {
            value
                .Append('\n')
                .Append(item.ProductId.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(item.Quantity.ToString(CultureInfo.InvariantCulture));
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString())));
    }
}
