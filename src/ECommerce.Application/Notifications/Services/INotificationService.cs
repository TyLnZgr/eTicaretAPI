using ECommerce.Application.Common.Pagination;
using ECommerce.Application.Notifications.Dtos;

namespace ECommerce.Application.Notifications.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetAllAsync(
        Guid customerId,
        NotificationQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        Guid id,
        Guid customerId,
        CancellationToken cancellationToken = default);
}
