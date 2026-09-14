namespace ECommerce.Api.Features.Notifications.Dtos;

public sealed class NotificationQueryParameters
{
    public bool? UnreadOnly { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}
