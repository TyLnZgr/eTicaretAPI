using System.Net.Mail;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Orders;

namespace ECommerce.Application.Orders.Validation;

public static class OrderRequestValidator
{
    public static Dictionary<string, string[]> ValidateCreate(
        CreateOrderRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.AddressId <= 0)
        {
            errors["addressId"] = new[]
            {
                "A valid shipping address ID is required."
            };
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = new[]
            {
                "An order must contain at least one item."
            };

            return errors;
        }

        if (request.Items.Count > Order.MaxItemCount)
        {
            errors["items"] = new[]
            {
                $"An order cannot contain more than {Order.MaxItemCount} items."
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

            if (item.Quantity < 1 || item.Quantity > OrderItem.MaxQuantity)
            {
                errors[$"items[{index}].quantity"] = new[]
                {
                    $"Quantity must be between 1 and {OrderItem.MaxQuantity}."
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
        return ValidateQueryCore(
            queryParameters.Status,
            queryParameters.CreatedFrom,
            queryParameters.CreatedTo,
            queryParameters.Page,
            queryParameters.PageSize);
    }

    public static Dictionary<string, string[]> ValidateAdminQuery(
        AdminOrderQueryParameters queryParameters)
    {
        var errors = ValidateQueryCore(
            queryParameters.Status,
            queryParameters.CreatedFrom,
            queryParameters.CreatedTo,
            queryParameters.Page,
            queryParameters.PageSize);

        if (queryParameters.CustomerId == Guid.Empty)
        {
            errors["customerId"] = new[]
            {
                "Customer ID cannot be an empty GUID."
            };
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.CustomerEmail))
        {
            var email = queryParameters.CustomerEmail.Trim();

            if (email.Length > 254 ||
                !MailAddress.TryCreate(email, out _))
            {
                errors["customerEmail"] = new[]
                {
                    "Customer email filter must be a valid email address."
                };
            }
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateQueryCore(
        string? statusValue,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        int? pageValue,
        int? pageSizeValue)
    {
        var errors = new Dictionary<string, string[]>();

        if (!string.IsNullOrWhiteSpace(statusValue) &&
            !TryParseStatus(statusValue, out _))
        {
            errors["status"] = new[]
            {
                "Status must be Pending, Paid, Shipped, Completed, or Cancelled."
            };
        }

        if (createdFrom.HasValue &&
            createdTo.HasValue &&
            createdFrom.Value > createdTo.Value)
        {
            errors["createdRange"] = new[]
            {
                "Created-from date cannot be later than created-to date."
            };
        }

        var page = pageValue ?? 1;
        var pageSize = pageSizeValue ?? 20;

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

        return wasParsed &&
            Enum.IsDefined(status) &&
            status != OrderStatus.PaymentProcessing;
    }
}
