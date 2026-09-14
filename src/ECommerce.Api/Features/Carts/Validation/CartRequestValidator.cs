using ECommerce.Api.Features.Carts.Dtos;

namespace ECommerce.Api.Features.Carts.Validation;

public static class CartRequestValidator
{
    public const int MaximumQuantityPerItem = 1_000;

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
            request.Quantity > MaximumQuantityPerItem)
        {
            errors["quantity"] = new[]
            {
                $"Quantity must be between 1 and {MaximumQuantityPerItem}."
            };
        }

        return errors;
    }

    public static Dictionary<string, string[]> ValidateCheckout(
        CheckoutCartRequest request)
    {
        var errors = new Dictionary<string, string[]>();

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
