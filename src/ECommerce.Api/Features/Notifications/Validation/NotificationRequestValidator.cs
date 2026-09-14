using ECommerce.Api.Features.Notifications.Dtos;

namespace ECommerce.Api.Features.Notifications.Validation;

public static class NotificationRequestValidator
{
    public static Dictionary<string, string[]> ValidateQuery(
        NotificationQueryParameters queryParameters)
    {
        var errors = new Dictionary<string, string[]>();

        if (queryParameters.Page is <= 0)
        {
            errors["page"] = new[]
            {
                "Page must be greater than zero."
            };
        }

        if (queryParameters.PageSize is <= 0 or > 100)
        {
            errors["pageSize"] = new[]
            {
                "Page size must be between 1 and 100."
            };
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;
        var offset = ((long)page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            errors["page"] = new[]
            {
                "Requested page is too large."
            };
        }

        return errors;
    }
}
