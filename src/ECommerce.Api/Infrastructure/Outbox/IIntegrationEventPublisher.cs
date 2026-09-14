namespace ECommerce.Api.Infrastructure.Outbox;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default);
}
