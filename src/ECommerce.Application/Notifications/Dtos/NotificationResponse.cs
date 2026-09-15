namespace ECommerce.Application.Notifications.Dtos;

public sealed record NotificationResponse(
    Guid Id,
    int? OrderId,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
