using ECommerce.Domain.Notifications;

namespace ECommerce.Api.Tests.Domain.Notifications;

public sealed class CustomerNotificationTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 9, 15, 17, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime ReadAtUtc =
        CreatedAtUtc.AddMinutes(1);

    [Fact]
    public void Constructor_WhenValuesAreValid_NormalizesNotification()
    {
        var customerId = Guid.NewGuid();
        var sourceMessageId = Guid.NewGuid();

        var notification = new CustomerNotification(
            customerId,
            orderId: 10,
            sourceMessageId,
            NotificationType.OrderPaid,
            "  Payment received  ",
            "  Your order payment was received.  ",
            CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(customerId, notification.CustomerId);
        Assert.Equal(10, notification.OrderId);
        Assert.Equal(sourceMessageId, notification.SourceMessageId);
        Assert.Equal(NotificationType.OrderPaid, notification.Type);
        Assert.Equal("Payment received", notification.Title);
        Assert.Equal(
            "Your order payment was received.",
            notification.Message);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
        Assert.Equal(CreatedAtUtc, notification.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_WhenNotificationTypeIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CustomerNotification(
                Guid.NewGuid(),
                orderId: 10,
                Guid.NewGuid(),
                (NotificationType)999,
                "Payment received",
                "Your order payment was received.",
                CreatedAtUtc));
    }

    [Fact]
    public void MarkAsRead_WhenUnread_ChangesReadState()
    {
        var notification = CreateNotification();

        var wasChanged = notification.MarkAsRead(ReadAtUtc);

        Assert.True(wasChanged);
        Assert.True(notification.IsRead);
        Assert.Equal(ReadAtUtc, notification.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotent()
    {
        var notification = CreateNotification();
        notification.MarkAsRead(ReadAtUtc);

        var wasChanged = notification.MarkAsRead(
            ReadAtUtc.AddMinutes(5));

        Assert.False(wasChanged);
        Assert.Equal(ReadAtUtc, notification.ReadAtUtc);
    }

    private static CustomerNotification CreateNotification()
    {
        return new CustomerNotification(
            Guid.NewGuid(),
            orderId: 10,
            Guid.NewGuid(),
            NotificationType.OrderPaid,
            "Payment received",
            "Your order payment was received.",
            CreatedAtUtc);
    }
}
