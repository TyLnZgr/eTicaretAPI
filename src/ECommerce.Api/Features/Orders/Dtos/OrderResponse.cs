namespace ECommerce.Api.Features.Orders.Dtos;

public sealed record OrderResponse(
    int Id,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);
