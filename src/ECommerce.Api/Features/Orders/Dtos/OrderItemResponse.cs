namespace ECommerce.Api.Features.Orders.Dtos;

public sealed record OrderItemResponse(
    int Id,
    int? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);
