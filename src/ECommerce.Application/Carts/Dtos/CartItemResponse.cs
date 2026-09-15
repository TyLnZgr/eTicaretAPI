namespace ECommerce.Application.Carts.Dtos;

public sealed record CartItemResponse(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int AvailableStock,
    bool IsActive,
    bool IsAvailable);
