namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class InProcessIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InProcessIntegrationEventPublisher> _logger;

    public InProcessIntegrationEventPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<InProcessIntegrationEventPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task PublishAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var handlers = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler>()
            .Where(handler => handler.EventType == message.Type)
            .ToArray();

        if (handlers.Length == 0)
        {
            throw new InvalidOperationException(
                $"No integration event handler is registered for '{message.Type}'.");
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(message, cancellationToken);
        }

        _logger.LogInformation(
            "Integration event {EventType} with message ID {MessageId} " +
            "was dispatched to {HandlerCount} handler(s).",
            message.Type,
            message.MessageId,
            handlers.Length);
    }
}
