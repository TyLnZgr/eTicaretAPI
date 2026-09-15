namespace ECommerce.Infrastructure.Messaging.Outbox;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default);
}
