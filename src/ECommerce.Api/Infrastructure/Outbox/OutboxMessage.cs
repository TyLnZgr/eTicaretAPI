namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public const int MaximumTypeLength = 200;
    public const int MaximumDeduplicationKeyLength = 200;
    public const int MaximumErrorLength = 2000;

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

    public void MarkProcessed(DateTime processedAtUtc)
    {
        EnsureNotProcessed();

        AttemptCount++;
        ProcessedAtUtc = processedAtUtc;
        NextAttemptAtUtc = null;
        LastError = null;
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
    }

    private void EnsureNotProcessed()
    {
        if (ProcessedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "A processed outbox message cannot be changed.");
        }
    }
}
