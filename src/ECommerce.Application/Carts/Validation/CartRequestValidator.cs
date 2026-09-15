using ECommerce.Application.Carts.Dtos;
using ECommerce.Domain.Carts;

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
