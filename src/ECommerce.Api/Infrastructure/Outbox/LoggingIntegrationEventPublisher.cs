namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class LoggingIntegrationEventPublisher
    : IIntegrationEventPublisher
{
    private readonly ILogger<LoggingIntegrationEventPublisher> _logger;

    public LoggingIntegrationEventPublisher(
        ILogger<LoggingIntegrationEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(
        OutboxEnvelope message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Integration event {EventType} with message ID {MessageId} " +
            "was published.",
            message.Type,
            message.MessageId);

        return Task.CompletedTask;
    }
}
