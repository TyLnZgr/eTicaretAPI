using ECommerce.Application.Common.Pagination;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Operations.Outbox.Dtos;
using ECommerce.Infrastructure.Operations.Outbox.Mappings;
using ECommerce.Infrastructure.Operations.Outbox.Outcomes;
using ECommerce.Infrastructure.Operations.Outbox.Validation;
using ECommerce.Infrastructure.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Operations.Outbox.Services;

public sealed class EfCoreOutboxAdministrationService
    : IOutboxAdministrationService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreOutboxAdministrationService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<OutboxMessageResponse>> GetAllAsync(
        OutboxMessageQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var query = _dbContext.OutboxMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(queryParameters.Type))
        {
            var type = queryParameters.Type.Trim();
            query = query.Where(message => message.Type == type);
        }

        if (OutboxAdministrationValidator.TryParseStatus(
                queryParameters.Status,
                out var status))
        {
            query = ApplyStatusFilter(query, status, now);
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var skip = checked((int)(((long)page - 1) * pageSize));
        var totalCount = await query.CountAsync(cancellationToken);

        var messages = await query
            .OrderByDescending(message => message.OccurredAtUtc)
            .ThenByDescending(message => message.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<OutboxMessageResponse>(
            messages
                .Select(message =>
                    message.ToAdministrationResponse(now))
                .ToArray(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<OutboxRetryStatus> RetryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken);

        if (message is null)
        {
            return OutboxRetryStatus.MessageNotFound;
        }

        if (!message.DeadLetteredAtUtc.HasValue)
        {
            return OutboxRetryStatus.MessageNotDeadLettered;
        }

        message.Retry(_timeProvider.GetUtcNow().UtcDateTime);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return OutboxRetryStatus.Success;
    }

    private static IQueryable<OutboxMessage> ApplyStatusFilter(
        IQueryable<OutboxMessage> query,
        OutboxMessageState status,
        DateTime now)
    {
        return status switch
        {
            OutboxMessageState.Processed => query.Where(message =>
                message.ProcessedAtUtc != null),
            OutboxMessageState.DeadLettered => query.Where(message =>
                message.DeadLetteredAtUtc != null),
            OutboxMessageState.Processing => query.Where(message =>
                message.ProcessedAtUtc == null &&
                message.DeadLetteredAtUtc == null &&
                message.LockId != null &&
                message.LockedUntilUtc > now),
            OutboxMessageState.Retrying => query.Where(message =>
                message.ProcessedAtUtc == null &&
                message.DeadLetteredAtUtc == null &&
                message.AttemptCount > 0 &&
                (message.LockedUntilUtc == null ||
                 message.LockedUntilUtc <= now)),
            _ => query.Where(message =>
                message.ProcessedAtUtc == null &&
                message.DeadLetteredAtUtc == null &&
                message.AttemptCount == 0 &&
                (message.LockedUntilUtc == null ||
                 message.LockedUntilUtc <= now))
        };
    }
}
