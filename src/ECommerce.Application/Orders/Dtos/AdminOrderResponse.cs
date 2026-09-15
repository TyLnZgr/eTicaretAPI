namespace ECommerce.Application.Orders.Dtos;

public sealed record AdminOrderResponse(
    int Id,
    Guid? CustomerId,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAtUtc,
    OrderAddressResponse? ShippingAddress,
    IReadOnlyList<OrderItemResponse> Items);
