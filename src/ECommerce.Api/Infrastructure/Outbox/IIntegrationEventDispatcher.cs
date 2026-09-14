namespace ECommerce.Api.Infrastructure.Outbox;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default);
}
