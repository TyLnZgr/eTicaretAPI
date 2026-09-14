using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Orders.Validation;

public static class OrderRequestValidator
{
    private const int MaximumItemCount = 100;
    private const int MaximumQuantityPerItem = 1_000;

    public static Dictionary<string, string[]> ValidateCreate(
        CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = new[]
            {
                "An order must contain at least one item."
            };

            return errors;
        }

        if (request.Items.Count > MaximumItemCount)
        {
            errors["items"] = new[]
            {
                $"An order cannot contain more than {MaximumItemCount} items."
            };
        }

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];

            if (item.ProductId <= 0)
            {
                errors[$"items[{index}].productId"] = new[]
                {
                    "A valid product ID is required."
                };
            }

            if (item.Quantity < 1 || item.Quantity > MaximumQuantityPerItem)
            {
                errors[$"items[{index}].quantity"] = new[]
                {
                    $"Quantity must be between 1 and {MaximumQuantityPerItem}."
                };
            }
        }

        var hasDuplicateProducts = request.Items
            .GroupBy(item => item.ProductId)
            .Any(group => group.Count() > 1);

        if (hasDuplicateProducts)
        {
            errors["items"] = new[]
            {
                "The same product cannot appear more than once in an order."
            };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateQuery(
        OrderQueryParameters queryParameters)
    {
        var errors = new Dictionary<string, string[]>();

        if (!string.IsNullOrWhiteSpace(queryParameters.Status) &&
            !TryParseStatus(queryParameters.Status, out _))
        {
            errors["status"] = new[]
            {
                "Status must be Pending, Paid, Shipped, Completed, or Cancelled."
            };
        }

        if (queryParameters.CreatedFrom.HasValue &&
            queryParameters.CreatedTo.HasValue &&
            queryParameters.CreatedFrom.Value > queryParameters.CreatedTo.Value)
        {
            errors["createdRange"] = new[]
            {
                "Created-from date cannot be later than created-to date."
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

        if (pageSize < 1 || pageSize > 100)
        {
            errors["pageSize"] = new[]
            {
                "Page size must be between 1 and 100."
            };
        }

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

    public static bool TryParseStatus(
        UpdateOrderStatusRequest request,
        out OrderStatus status)
    {
        return TryParseStatus(request.Status, out status);
    }

    public static bool TryParseStatus(
        string? value,
        out OrderStatus status)
    {
        if (int.TryParse(value, out _))
        {
            status = default;
            return false;
        }

        var wasParsed = Enum.TryParse(
            value?.Trim(),
            ignoreCase: true,
            out status);

        return wasParsed && Enum.IsDefined(status);
    }
}
