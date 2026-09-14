using ECommerce.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class EfCoreOutboxProcessor : IOutboxProcessor
{
    private readonly ECommerceDbContext _dbContext;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<EfCoreOutboxProcessor> _logger;

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
    }

    public async Task<OutboxProcessingResult> ProcessPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var messages = await _dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .Where(message =>
                message.NextAttemptAtUtc == null ||
                message.NextAttemptAtUtc <= now)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(_options.BatchSize)
            .ToArrayAsync(cancellationToken);

        var processedCount = 0;
        var failedCount = 0;

        foreach (var message in messages)
        {
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
                var attemptNumber = message.AttemptCount + 1;
                var retryDelay = CalculateRetryDelay(attemptNumber);

                message.MarkFailed(
                    exception.Message,
                    _timeProvider.GetUtcNow().UtcDateTime,
                    retryDelay);

                await _dbContext.SaveChangesAsync(cancellationToken);
                failedCount++;

                _logger.LogWarning(
                    exception,
                    "Outbox message {MessageId} could not be published. " +
                    "Attempt {AttemptNumber} will be retried after " +
                    "{RetryDelay}.",
                    message.Id,
                    attemptNumber,
                    retryDelay);

                continue;
            }

            message.MarkProcessed(
                _timeProvider.GetUtcNow().UtcDateTime);

            await _dbContext.SaveChangesAsync(cancellationToken);
            processedCount++;
        }

        return new OutboxProcessingResult(
            messages.Length,
            processedCount,
            failedCount);
    }

    private static TimeSpan CalculateRetryDelay(int attemptNumber)
    {
        var exponent = Math.Min(attemptNumber, 8);
        var seconds = Math.Min(Math.Pow(2, exponent), 300);

        return TimeSpan.FromSeconds(seconds);
    }
}
