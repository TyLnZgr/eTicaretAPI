using ECommerce.Api.Features.Notifications.Dtos;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Notifications.Mappings;

public static class NotificationMappings
{
    public static NotificationResponse ToResponse(
        this CustomerNotification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.OrderId,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
    }
}
