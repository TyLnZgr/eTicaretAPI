using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Notifications.Dtos;
using ECommerce.Application.Notifications.Mappings;
using ECommerce.Application.Notifications.Services;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Services.Notifications;

public sealed class EfCoreNotificationService : INotificationService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreNotificationService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<NotificationResponse>> GetAllAsync(
        Guid customerId,
        NotificationQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CustomerNotifications
            .AsNoTracking()
            .Where(notification =>
                notification.CustomerId == customerId);

        if (queryParameters.UnreadOnly == true)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var skip = checked((int)(((long)page - 1) * pageSize));
        var totalCount = await query.CountAsync(cancellationToken);

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<NotificationResponse>(
            notifications
                .Select(notification => notification.ToResponse())
                .ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<bool> MarkAsReadAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.CustomerNotifications
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == id &&
                    candidate.CustomerId == customerId,
                cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.MarkAsRead(
            _timeProvider.GetUtcNow().UtcDateTime);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
