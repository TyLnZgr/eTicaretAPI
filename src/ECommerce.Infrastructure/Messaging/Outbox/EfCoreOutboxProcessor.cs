using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed class EfCoreOutboxProcessor : IOutboxProcessor
{
    private readonly ECommerceDbContext _dbContext;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<EfCoreOutboxProcessor> _logger;
    private readonly string _workerName;

    public EfCoreOutboxProcessor(
        ECommerceDbContext dbContext,
        IIntegrationEventPublisher publisher,
        TimeProvider timeProvider,
        IOptions<OutboxOptions> options,
        ILogger<EfCoreOutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
        _workerName = $"{Environment.MachineName}:{Environment.ProcessId}";
    }

    public async Task<OutboxProcessingResult> ProcessPendingAsync(
        CancellationToken cancellationToken = default)
    {
        _dbContext.ChangeTracker.Clear();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var messages = await ClaimMessagesAsync(
            now,
            cancellationToken);

        var processedCount = 0;
        var failedCount = 0;
        var deadLetteredCount = 0;
        var leaseLostCount = 0;

        foreach (var message in messages)
        {
            Exception? publishException = null;

            try
            {
                await _publisher.PublishAsync(
                    new OutboxEnvelope(
                        message.Id,
                        message.Type,
                        message.Payload,
                        message.OccurredAtUtc),
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                publishException = exception;
            }

            if (publishException is not null)
            {
                var attemptNumber = message.AttemptCount + 1;
                var failureTime = _timeProvider.GetUtcNow().UtcDateTime;

                if (attemptNumber >= _options.MaximumAttempts)
                {
                    message.MarkDeadLettered(
                        publishException.Message,
                        failureTime);
                }
                else
                {
                    message.MarkFailed(
                        publishException.Message,
                        failureTime,
                        CalculateRetryDelay(attemptNumber));
                }

                if (!await TrySaveClaimResultAsync(
                        message,
                        cancellationToken))
                {
                    leaseLostCount++;
                    continue;
                }

                if (message.DeadLetteredAtUtc.HasValue)
                {
                    deadLetteredCount++;

                    _logger.LogError(
                        publishException,
                        "Outbox message {MessageId} was moved to the " +
                        "dead-letter state after {AttemptCount} attempts.",
                        message.Id,
                        message.AttemptCount);
                }
                else
                {
                    failedCount++;

                    _logger.LogWarning(
                        publishException,
                        "Outbox message {MessageId} could not be published. " +
                        "Attempt {AttemptNumber} will be retried at " +
                        "{NextAttemptAtUtc}.",
                        message.Id,
                        attemptNumber,
                        message.NextAttemptAtUtc);
                }

                _dbContext.Entry(message).State = EntityState.Detached;
                continue;
            }

            message.MarkProcessed(
                _timeProvider.GetUtcNow().UtcDateTime);

            if (!await TrySaveClaimResultAsync(
                    message,
                    cancellationToken))
            {
                leaseLostCount++;
                continue;
            }

            processedCount++;
            _dbContext.Entry(message).State = EntityState.Detached;
        }

        return new OutboxProcessingResult(
            messages.Length,
            processedCount,
            failedCount,
            deadLetteredCount,
            leaseLostCount);
    }

    private async Task<OutboxMessage[]> ClaimMessagesAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var candidateIds = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc == null)
            .Where(message => message.DeadLetteredAtUtc == null)
            .Where(message =>
                message.NextAttemptAtUtc == null ||
                message.NextAttemptAtUtc <= now)
            .Where(message =>
                message.LockedUntilUtc == null ||
                message.LockedUntilUtc <= now)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(_options.BatchSize)
            .Select(message => message.Id)
            .ToArrayAsync(cancellationToken);

        if (candidateIds.Length == 0)
        {
            return [];
        }

        var lockId = Guid.NewGuid();
        var lockedUntilUtc = now.Add(_options.LeaseDuration);

        await _dbContext.OutboxMessages
            .Where(message => candidateIds.Contains(message.Id))
            .Where(message => message.ProcessedAtUtc == null)
            .Where(message => message.DeadLetteredAtUtc == null)
            .Where(message =>
                message.NextAttemptAtUtc == null ||
                message.NextAttemptAtUtc <= now)
            .Where(message =>
                message.LockedUntilUtc == null ||
                message.LockedUntilUtc <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.LockId, lockId)
                    .SetProperty(message => message.LockedBy, _workerName)
                    .SetProperty(
                        message => message.LockedUntilUtc,
                        lockedUntilUtc),
                cancellationToken);

        return await _dbContext.OutboxMessages
            .Where(message => message.LockId == lockId)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<bool> TrySaveClaimResultAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _dbContext.Entry(message).State = EntityState.Detached;

            _logger.LogWarning(
                exception,
                "Lease ownership was lost for outbox message {MessageId}; " +
                "the stale result was not persisted.",
                message.Id);

            return false;
        }
    }

    private static TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        var exponent = Math.Min(attemptNumber, 8);
        var seconds = Math.Min(Math.Pow(2, exponent), 300);

        return TimeSpan.FromSeconds(seconds);
    }
}
