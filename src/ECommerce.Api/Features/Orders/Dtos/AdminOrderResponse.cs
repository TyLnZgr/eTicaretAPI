namespace ECommerce.Api.Features.Orders.Dtos;

public sealed record AdminOrderResponse(
    int Id,
    Guid? CustomerId,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);
