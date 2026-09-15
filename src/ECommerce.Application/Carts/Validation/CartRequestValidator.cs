using ECommerce.Application.Carts.Dtos;
using ECommerce.Domain.Carts;
using ECommerce.Domain.Orders;

namespace ECommerce.Application.Carts.Validation;

public static class CartRequestValidator
{
    public static Dictionary<string, string[]> ValidateSetQuantity(
        int productId,
        SetCartItemQuantityRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (productId <= 0)
        {
            errors["productId"] = new[]
            {
                "A valid product ID is required."
            };
        }

        if (request.Quantity < 1 ||
            request.Quantity > CartItem.MaximumQuantity)
        {
            errors["quantity"] = new[]
            {
                $"Quantity must be between 1 and {CartItem.MaximumQuantity}."
            };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCheckout(
        string? idempotencyKey,
        CheckoutCartRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!Order.IsIdempotencyKeyValid(idempotencyKey))
        {
            errors["Idempotency-Key"] = new[]
            {
                $"Idempotency-Key must be between " +
                $"{Order.IdempotencyKeyMinLength} and " +
                $"{Order.IdempotencyKeyMaxLength} characters and contain " +
                "only letters, digits, hyphens, underscores, dots, or colons."
            };
        }

        if (request.AddressId <= 0)
        {
            errors["addressId"] = new[]
            {
                "A valid shipping address ID is required."
            };
        }

        return errors;
    }
}
