namespace ECommerce.Infrastructure.Messaging.Outbox;

public interface IOutboxProcessor
{
    Task<OutboxProcessingResult> ProcessPendingAsync(
        CancellationToken cancellationToken = default);
}
