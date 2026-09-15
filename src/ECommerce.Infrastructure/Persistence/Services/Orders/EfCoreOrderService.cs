using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Mappings;
using ECommerce.Application.Orders.Outcomes;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Orders.Validation;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using ECommerce.Domain.Orders;
namespace ECommerce.Infrastructure.Persistence.Services.Orders;

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
        Guid customerId,
        OrderQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == customerId);

        query = ApplyOrderFilters(
            query,
            queryParameters.Status,
            queryParameters.CreatedFrom,
            queryParameters.CreatedTo);

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var (orders, totalCount) = await LoadOrderPageAsync(
            query,
            page,
            pageSize,
            cancellationToken);

        var items = orders
            .Select(order => order.ToResponse())
            .ToArray();

        return new PagedResult<OrderResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<PagedResult<AdminOrderResponse>>
        GetAllAsAdministratorAsync(
            AdminOrderQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders.AsNoTracking();

        if (queryParameters.CustomerId.HasValue)
        {
            query = query.Where(order =>
                order.CustomerId == queryParameters.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.CustomerEmail))
        {
            var customerEmail = queryParameters.CustomerEmail
                .Trim()
                .ToLowerInvariant();

            query = query.Where(order =>
                order.CustomerEmail == customerEmail);
        }

        query = ApplyOrderFilters(
            query,
            queryParameters.Status,
            queryParameters.CreatedFrom,
            queryParameters.CreatedTo);

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var (orders, totalCount) = await LoadOrderPageAsync(
            query,
            page,
            pageSize,
            cancellationToken);

        var items = orders
            .Select(order => order.ToAdminResponse())
            .ToArray();

        return new PagedResult<AdminOrderResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<Order?> GetByIdAsync(
        int id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(
                order =>
                    order.Id == id &&
                    order.CustomerId == customerId,
                cancellationToken);
    }

    public Task<OrderStatusUpdateResult> UpdateStatusAsync(
        int id,
        Guid customerId,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        return UpdateStatusCoreAsync(
            id,
            customerId,
            newStatus,
            cancellationToken);
    }

    public Task<OrderStatusUpdateResult> UpdateStatusAsAdministratorAsync(
        int id,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        return UpdateStatusCoreAsync(
            id,
            customerId: null,
            newStatus,
            cancellationToken);
    }

    private async Task<OrderStatusUpdateResult> UpdateStatusCoreAsync(
        int id,
        Guid? customerId,
        OrderStatus newStatus,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var orderQuery = _dbContext.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .Where(candidate => candidate.Id == id);

        if (customerId.HasValue)
        {
            orderQuery = orderQuery.Where(candidate =>
                candidate.CustomerId == customerId.Value);
        }

        var order = await orderQuery.SingleOrDefaultAsync(
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

        var updateQuery = _dbContext.Orders
            .Where(candidate => candidate.Id == id)
            .Where(candidate => candidate.Status == order.Status);

        if (customerId.HasValue)
        {
            updateQuery = updateQuery.Where(candidate =>
                candidate.CustomerId == customerId.Value);
        }

        var affectedOrders = await updateQuery
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

        var updatedOrderQuery = _dbContext.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .Where(candidate => candidate.Id == id);

        if (customerId.HasValue)
        {
            updatedOrderQuery = updatedOrderQuery.Where(candidate =>
                candidate.CustomerId == customerId.Value);
        }

        var updatedOrder = await updatedOrderQuery.SingleOrDefaultAsync(
            cancellationToken);

        return new OrderStatusUpdateResult(
            OrderStatusUpdateStatus.Success,
            updatedOrder);
    }

    private static IQueryable<Order> ApplyOrderFilters(
        IQueryable<Order> query,
        string? statusValue,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo)
    {
        if (!string.IsNullOrWhiteSpace(statusValue) &&
            OrderRequestValidator.TryParseStatus(
                statusValue,
                out var status))
        {
            query = query.Where(order => order.Status == status);
        }

        if (createdFrom.HasValue)
        {
            var createdFromUtc = createdFrom.Value.UtcDateTime;

            query = query.Where(order =>
                order.CreatedAtUtc >= createdFromUtc);
        }

        if (createdTo.HasValue)
        {
            var createdToUtc = createdTo.Value.UtcDateTime;

            query = query.Where(order =>
                order.CreatedAtUtc <= createdToUtc);
        }

        return query;
    }

    private static async Task<(List<Order> Orders, int TotalCount)>
        LoadOrderPageAsync(
            IQueryable<Order> query,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var orders = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenByDescending(order => order.Id)
            .Skip(skip)
            .Take(pageSize)
            .Include(order => order.Items)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }
}
