namespace ECommerce.Infrastructure.Operations.Outbox.Dtos;

public sealed record OutboxMessageResponse(
    Guid Id,
    string Type,
    string DeduplicationKey,
    string Status,
    DateTime OccurredAtUtc,
    DateTime? ProcessedAtUtc,
    int AttemptCount,
    DateTime? NextAttemptAtUtc,
    string? LastError,
    string? LockedBy,
    DateTime? LockedUntilUtc,
    DateTime? DeadLetteredAtUtc);
