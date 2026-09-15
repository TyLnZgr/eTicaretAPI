namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed class OutboxMessage
{
    public const int MaximumTypeLength = 200;
    public const int MaximumDeduplicationKeyLength = 200;
    public const int MaximumErrorLength = 2000;
    public const int MaximumWorkerNameLength = 200;

    private OutboxMessage()
    {
    }

    public OutboxMessage(
        string type,
        string deduplicationKey,
        string payload,
        DateTime occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var normalizedType = type.Trim();
        var normalizedDeduplicationKey = deduplicationKey.Trim();

        if (normalizedType.Length > MaximumTypeLength)
        {
            throw new ArgumentException(
                $"The event type cannot exceed {MaximumTypeLength} characters.",
                nameof(type));
        }

        if (normalizedDeduplicationKey.Length >
            MaximumDeduplicationKeyLength)
        {
            throw new ArgumentException(
                "The deduplication key cannot exceed " +
                $"{MaximumDeduplicationKeyLength} characters.",
                nameof(deduplicationKey));
        }

        Id = Guid.NewGuid();
        Type = normalizedType;
        DeduplicationKey = normalizedDeduplicationKey;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string DeduplicationKey { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public Guid? LockId { get; private set; }
    public string? LockedBy { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public DateTime? DeadLetteredAtUtc { get; private set; }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        EnsureNotProcessed();

        AttemptCount++;
        ProcessedAtUtc = processedAtUtc;
        NextAttemptAtUtc = null;
        LastError = null;
        ClearLease();
    }

    public void MarkFailed(
        string error,
        DateTime failedAtUtc,
        TimeSpan retryDelay)
    {
        EnsureNotProcessed();
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        if (retryDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retryDelay),
                "The retry delay must be positive.");
        }

        AttemptCount++;
        LastError = error.Length <= MaximumErrorLength
            ? error
            : error[..MaximumErrorLength];
        NextAttemptAtUtc = failedAtUtc.Add(retryDelay);
        ClearLease();
    }

    public void MarkDeadLettered(
        string error,
        DateTime deadLetteredAtUtc)
    {
        EnsureNotProcessed();
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        AttemptCount++;
        DeadLetteredAtUtc = deadLetteredAtUtc;
        LastError = error.Length <= MaximumErrorLength
            ? error
            : error[..MaximumErrorLength];
        NextAttemptAtUtc = null;
        ClearLease();
    }

    public void Retry(DateTime nextAttemptAtUtc)
    {
        if (!DeadLetteredAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Only a dead-lettered outbox message can be retried manually.");
        }

        AttemptCount = 0;
        NextAttemptAtUtc = nextAttemptAtUtc;
        LastError = null;
        DeadLetteredAtUtc = null;
        ClearLease();
    }

    private void EnsureNotProcessed()
    {
        if (ProcessedAtUtc.HasValue || DeadLetteredAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "A finalized outbox message cannot be changed.");
        }
    }

    private void ClearLease()
    {
        LockId = null;
        LockedBy = null;
        LockedUntilUtc = null;
    }
}
