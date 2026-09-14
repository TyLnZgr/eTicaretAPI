using ECommerce.Api.Features.Orders.Dtos;
using ECommerce.Api.Models;

namespace ECommerce.Api.Features.Orders.Mappings;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAtUtc,
            MapItems(order));
    }

    public static AdminOrderResponse ToAdminResponse(this Order order)
    {
        return new AdminOrderResponse(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAtUtc,
            MapItems(order));
    }

    private static OrderItemResponse[] MapItems(Order order)
    {
        return order.Items
            .OrderBy(item => item.Id)
            .Select(item => new OrderItemResponse(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal))
            .ToArray();
    }
}
