namespace ECommerce.Infrastructure.Messaging.Outbox;

public interface IIntegrationEventHandler
{
    string EventType { get; }

    Task HandleAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default);
}
