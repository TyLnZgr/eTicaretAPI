namespace ECommerce.Infrastructure.Messaging.Outbox;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default);
}
