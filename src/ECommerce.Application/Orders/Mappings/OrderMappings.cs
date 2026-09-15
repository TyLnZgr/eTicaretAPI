using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Orders;

namespace ECommerce.Application.Orders.Mappings;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmount,
            order.Currency,
            order.CreatedAtUtc,
            MapShippingAddress(order.ShippingAddress),
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
            order.Currency,
            order.CreatedAtUtc,
            MapShippingAddress(order.ShippingAddress),
            MapItems(order));
    }

    private static OrderAddressResponse? MapShippingAddress(
        OrderAddressSnapshot? address)
    {
        if (address is null)
        {
            return null;
        }

        return new OrderAddressResponse(
            address.RecipientFullName,
            address.PhoneNumber,
            address.AddressLine1,
            address.AddressLine2,
            address.District,
            address.City,
            address.PostalCode,
            address.CountryCode);
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
