namespace ECommerce.Api.Infrastructure.Outbox;

public interface IOutboxProcessor
{
    Task<OutboxProcessingResult> ProcessPendingAsync(
        CancellationToken cancellationToken = default);
}
