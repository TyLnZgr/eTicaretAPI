using ECommerce.Application.Carts.Dtos;
using ECommerce.Domain.Carts;

namespace ECommerce.Application.Carts.Mappings;

public static class CartMappings
{
    public static CartResponse ToResponse(this Cart cart)
    {
        var items = cart.Items
            .OrderBy(item => item.Product.Name)
            .ThenBy(item => item.ProductId)
            .Select(item =>
            {
                var lineTotal = item.Product.Price * item.Quantity;

                return new CartItemResponse(
                    item.ProductId,
                    item.Product.Name,
                    item.Product.Price,
                    item.Quantity,
                    lineTotal,
                    item.Product.StockQuantity,
                    item.Product.IsActive,
                    item.Product.IsActive &&
                    item.Product.StockQuantity >= item.Quantity);
            })
            .ToArray();

        return new CartResponse(
            items.Sum(item => item.Quantity),
            items.Sum(item => item.LineTotal),
            cart.UpdatedAtUtc,
            items);
    }
}
