using ECommerce.Api.Common.Pagination;
using ECommerce.Api.Features.Notifications.Dtos;

namespace ECommerce.Api.Features.Notifications.Services;

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
