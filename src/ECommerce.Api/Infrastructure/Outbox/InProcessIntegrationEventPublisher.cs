namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class InProcessIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private readonly IIntegrationEventDispatcher _dispatcher;

    public InProcessIntegrationEventPublisher(
        IIntegrationEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task PublishAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default)
    {
        await _dispatcher.DispatchAsync(message, cancellationToken);
    }
}
