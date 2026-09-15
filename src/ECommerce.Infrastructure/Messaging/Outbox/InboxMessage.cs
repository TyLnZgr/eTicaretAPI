namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed class InboxMessage
{
    public const int MaximumConsumerNameLength = 200;

    private InboxMessage()
    {
    }

    public InboxMessage(
        Guid messageId,
        string consumerName,
        DateTime processedAtUtc)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(
                "A message ID is required.",
                nameof(messageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        var normalizedConsumerName = consumerName.Trim();

        if (normalizedConsumerName.Length > MaximumConsumerNameLength)
        {
            throw new ArgumentException(
                "The consumer name cannot exceed " +
                $"{MaximumConsumerNameLength} characters.",
                nameof(consumerName));
        }

        MessageId = messageId;
        ConsumerName = normalizedConsumerName;
        ProcessedAtUtc = processedAtUtc;
    }

    public Guid MessageId { get; private set; }
    public string ConsumerName { get; private set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; private set; }
}
