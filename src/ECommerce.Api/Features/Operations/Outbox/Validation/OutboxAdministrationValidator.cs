using ECommerce.Api.Features.Operations.Outbox.Dtos;
using ECommerce.Api.Features.Operations.Outbox.Outcomes;
using ECommerce.Api.Infrastructure.Outbox;

namespace ECommerce.Api.Features.Operations.Outbox.Validation;

public static class OutboxAdministrationValidator
{
    public static Dictionary<string, string[]> ValidateQuery(
        OutboxMessageQueryParameters queryParameters)
    {
        var errors = new Dictionary<string, string[]>();

        if (!string.IsNullOrWhiteSpace(queryParameters.Status) &&
            !TryParseStatus(queryParameters.Status, out _))
        {
            errors["status"] = new[]
            {
                "Status must be Pending, Retrying, Processing, Processed, or DeadLettered."
            };
        }

        if (queryParameters.Type?.Trim().Length >
            OutboxMessage.MaximumTypeLength)
        {
            errors["type"] = new[]
            {
                $"Type cannot exceed {OutboxMessage.MaximumTypeLength} characters."
            };
        }

        var page = queryParameters.Page ?? 1;
        var pageSize = queryParameters.PageSize ?? 20;

        if (page < 1)
        {
            errors["page"] = new[]
            {
                "Page must be greater than zero."
            };
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = new[]
            {
                "Page size must be between 1 and 100."
            };
        }

        if (((long)page - 1) * pageSize > int.MaxValue)
        {
            errors["page"] = new[]
            {
                "Requested page is too large."
            };
        }

        return errors;
    }

    public static bool TryParseStatus(
        string? value,
        out OutboxMessageState status)
    {
        if (int.TryParse(value, out _))
        {
            status = default;
            return false;
        }

        return Enum.TryParse(
                value?.Trim(),
                ignoreCase: true,
                out status) &&
            Enum.IsDefined(status);
    }
}
