namespace ECommerce.Api.Features.Orders.Dtos;

public sealed record OrderResponse(
    int Id,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAtUtc,
    OrderAddressResponse? ShippingAddress,
    IReadOnlyList<OrderItemResponse> Items);
