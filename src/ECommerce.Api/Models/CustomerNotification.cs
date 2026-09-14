using ECommerce.Api.Identity;

namespace ECommerce.Api.Models;

public sealed class CustomerNotification
{
    public const int MaximumTitleLength = 200;
    public const int MaximumMessageLength = 1000;

    private CustomerNotification()
    {
    }

    public CustomerNotification(
        Guid customerId,
        int orderId,
        Guid sourceMessageId,
        NotificationType type,
        string title,
        string message,
        DateTime createdAtUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "A customer ID is required.",
                nameof(customerId));
        }

        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderId),
                "A valid order ID is required.");
        }

        if (sourceMessageId == Guid.Empty)
        {
            throw new ArgumentException(
                "A source message ID is required.",
                nameof(sourceMessageId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var normalizedTitle = title.Trim();
        var normalizedMessage = message.Trim();

        if (normalizedTitle.Length > MaximumTitleLength)
        {
            throw new ArgumentException(
                $"The title cannot exceed {MaximumTitleLength} characters.",
                nameof(title));
        }

        if (normalizedMessage.Length > MaximumMessageLength)
        {
            throw new ArgumentException(
                "The message cannot exceed " +
                $"{MaximumMessageLength} characters.",
                nameof(message));
        }

        Id = Guid.NewGuid();
        CustomerId = customerId;
        OrderId = orderId;
        SourceMessageId = sourceMessageId;
        Type = type;
        Title = normalizedTitle;
        Message = normalizedMessage;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public int? OrderId { get; private set; }
    public Guid SourceMessageId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public ApplicationUser Customer { get; private set; } = null!;
    public Order? Order { get; private set; }

    public void MarkAsRead(DateTime readAtUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = readAtUtc;
    }
}
