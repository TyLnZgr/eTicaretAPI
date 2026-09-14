using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Data;
using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Features.Orders.Mappings;
using ECommerce.Api.Features.Orders.Outcomes;
using ECommerce.Api.Features.Orders.Validation;
using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Orders.Services;

public sealed class EfCoreOrderService : IOrderService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreOrderService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<OrderResponse>> GetAllAsync(
        OrderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(queryParameters.CustomerEmail))
        {
            var customerEmail = queryParameters.CustomerEmail
                .Trim()
                .ToLowerInvariant();

            query = query.Where(order =>
                order.CustomerEmail == customerEmail);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Status) &&
            OrderRequestValidator.TryParseStatus(
                queryParameters.Status,
                out var status))
        {
            query = query.Where(order => order.Status == status);
        }

        if (queryParameters.CreatedFrom.HasValue)
        {
            var createdFromUtc =
                queryParameters.CreatedFrom.Value.UtcDateTime;

            query = query.Where(order =>
                order.CreatedAtUtc >= createdFromUtc);
        }

        if (queryParameters.CreatedTo.HasValue)
        {
            var createdToUtc =
                queryParameters.CreatedTo.Value.UtcDateTime;

            query = query.Where(order =>
                order.CreatedAtUtc <= createdToUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var skip = (page - 1) * pageSize;

        var orders = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .Skip(skip)
            .Take(pageSize)
            .Include(order => order.Items)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var items = orders
            .Select(order => order.ToResponse())
            .ToArray();

        return new PagedResult<OrderResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(
                order => order.Id == id,
                cancellationToken);
    }

    public async Task<OrderCreationResult> CreateAsync(
        string customerEmail,
        IReadOnlyList<CreateOrderItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerEmail) ||
            customerEmail.Trim().Length > 254 ||
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

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

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
                await transaction.RollbackAsync(cancellationToken);

                return new OrderCreationResult(
                    OrderCreationStatus.ProductNotFound,
                    ProductId: requestedItem.ProductId);
            }

            if (!product.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new OrderCreationResult(
                    OrderCreationStatus.ProductInactive,
                    ProductId: product.Id);
            }

            if (product.StockQuantity < requestedItem.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new OrderCreationResult(
                    OrderCreationStatus.InsufficientStock,
                    ProductId: product.Id);
            }
        }

        var order = new Order
        {
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

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

                await transaction.RollbackAsync(cancellationToken);

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
        await transaction.CommitAsync(cancellationToken);

        return new OrderCreationResult(
            OrderCreationStatus.Success,
            order);
    }

    public async Task<OrderStatusUpdateResult> UpdateStatusAsync(
        int id,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (order is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new OrderStatusUpdateResult(
                OrderStatusUpdateStatus.OrderNotFound);
        }

        if (!order.CanTransitionTo(newStatus))
        {
            await transaction.RollbackAsync(cancellationToken);

            return new OrderStatusUpdateResult(
                OrderStatusUpdateStatus.InvalidTransition,
                CurrentStatus: order.Status);
        }

        var affectedOrders = await _dbContext.Orders
            .Where(candidate => candidate.Id == id)
            .Where(candidate => candidate.Status == order.Status)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    candidate => candidate.Status,
                    newStatus),
                cancellationToken);

        if (affectedOrders == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new OrderStatusUpdateResult(
                OrderStatusUpdateStatus.ConcurrencyConflict,
                CurrentStatus: order.Status);
        }

        if (newStatus == OrderStatus.Cancelled)
        {
            var movementTime = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (var item in order.Items
                         .Where(item => item.ProductId.HasValue)
                         .OrderBy(item => item.ProductId))
            {
                var productId = item.ProductId!.Value;
                var quantityAsLong = (long)item.Quantity;

                var affectedProducts = await _dbContext.Products
                    .Where(product => product.Id == productId)
                    .Where(product =>
                        (long)product.StockQuantity + quantityAsLong <=
                        int.MaxValue)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            product => product.StockQuantity,
                            product => product.StockQuantity + item.Quantity),
                        cancellationToken);

                if (affectedProducts == 0)
                {
                    var productExists = await _dbContext.Products
                        .AsNoTracking()
                        .AnyAsync(
                            product => product.Id == productId,
                            cancellationToken);

                    if (!productExists)
                    {
                        continue;
                    }

                    await transaction.RollbackAsync(cancellationToken);

                    return new OrderStatusUpdateResult(
                        OrderStatusUpdateStatus.StockLimitExceeded,
                        CurrentStatus: order.Status,
                        ProductId: productId);
                }

                var stockQuantityAfter = await _dbContext.Products
                    .AsNoTracking()
                    .Where(product => product.Id == productId)
                    .Select(product => product.StockQuantity)
                    .SingleAsync(cancellationToken);

                _dbContext.StockMovements.Add(new StockMovement
                {
                    ProductId = productId,
                    QuantityDelta = item.Quantity,
                    StockQuantityAfter = stockQuantityAfter,
                    Reason = $"Order {order.Id} cancellation",
                    CreatedAtUtc = movementTime
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var updatedOrder = await GetByIdAsync(id, cancellationToken);

        return new OrderStatusUpdateResult(
            OrderStatusUpdateStatus.Success,
            updatedOrder);
    }
}
