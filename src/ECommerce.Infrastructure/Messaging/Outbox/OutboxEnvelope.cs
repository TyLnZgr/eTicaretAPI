namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed record OutboxEnvelope(
    Guid MessageId,
    string Type,
    string Payload,
    DateTime OccurredAtUtc);
