namespace ECommerce.Api.Infrastructure.Outbox;

public sealed record OutboxEnvelope(
    Guid MessageId,
    string Type,
    string Payload,
    DateTime OccurredAtUtc);
